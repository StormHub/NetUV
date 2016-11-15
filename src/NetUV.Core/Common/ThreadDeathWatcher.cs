// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using NetUV.Core.Logging;

    /// <summary>
    /// Forked from https://github.com/Azure/DotNetty
    /// </summary>
    static class ThreadDeathWatcher
    {
        static readonly ILog Log = LogFactory.ForContext(typeof(ThreadDeathWatcher));

        static readonly ConcurrentQueue<Entry> PendingEntries;
        static readonly Watcher WatcherInstance;
        static int started;

        static volatile Thread watcherThread;

        static ThreadDeathWatcher()
        {
            PendingEntries = new ConcurrentQueue<Entry>();
            WatcherInstance = new Watcher();
        }

        /// Schedules the specified task to run when the specified thread dies.
        public static void Watch(Thread thread, Action task)
        {
            Contract.Requires(thread != null);
            Contract.Requires(task != null);
            Contract.Requires(thread.IsAlive);

            Schedule(thread, task, true);
        }

        /// Cancels the task scheduled.
        public static void Unwatch(Thread thread, Action task)
        {
            Contract.Requires(thread != null);
            Contract.Requires(task != null);

            Schedule(thread, task, false);
        }

        static void Schedule(Thread thread, Action task, bool isWatch)
        {
            PendingEntries.Enqueue(new Entry(thread, task, isWatch));

            if (Interlocked.CompareExchange(ref started, 1, 0) == 0)
            {
                var newWatcherThread = new Thread(state => ((Watcher)state).Run());
                newWatcherThread.Start(WatcherInstance);
                watcherThread = newWatcherThread;
            }
        }

        /// Waits until the thread of this watcher has no threads to watch and terminates itself.
        /// Because a new watcher thread will be started again on thread, this operation is only 
        /// useful when you want to ensure that the watcher thread is terminated after your 
        /// application is shut down and there's no chance of calling thread afterwards.
        ///return true if and only if the watcher thread has been terminated
        public static bool AwaitInactivity(TimeSpan timeout)
        {
            Thread currentWatcher = watcherThread;
            if (currentWatcher != null)
            {
                currentWatcher.Join(timeout);
                return !currentWatcher.IsAlive;
            }
            else
            {
                return true;
            }
        }

        sealed class Watcher
        {
            readonly List<Entry> watchees = new List<Entry>();

            public void Run()
            {
                for (;;)
                {
                    this.FetchWatchees();
                    this.NotifyWatchees();

                    // Try once again just in case notifyWatchees() triggered watch() or unwatch().
                    this.FetchWatchees();
                    this.NotifyWatchees();

                    Thread.Sleep(1000);

                    if (this.watchees.Count == 0 && PendingEntries.IsEmpty)
                    {
                        // Mark the current worker thread as stopped.
                        // The following CAS must always success and must be uncontended,
                        // because only one watcher thread should be running at the same time.
                        bool stopped = Interlocked.CompareExchange(ref started, 0, 1) == 1;
                        Contract.Assert(stopped);

                        // Check if there are pending entries added by watch() while we do CAS above.
                        if (PendingEntries.IsEmpty)
                        {
                            // A) watch() was not invoked and thus there's nothing to handle
                            //    -> safe to terminate because there's nothing left to do
                            // B) a new watcher thread started and handled them all
                            //    -> safe to terminate the new watcher thread will take care the rest
                            break;
                        }

                        // There are pending entries again, added by watch()
                        if (Interlocked.CompareExchange(ref started, 1, 0) != 0)
                        {
                            // watch() started a new watcher thread and set 'started' to true.
                            // -> terminate this thread so that the new watcher reads from pendingEntries exclusively.
                            break;
                        }

                        // watch() added an entry, but this worker was faster to set 'started' to true.
                        // i.e. a new watcher thread was not started
                        // -> keep this thread alive to handle the newly added entries.
                    }
                }
            }

            void FetchWatchees()
            {
                for (;;)
                {
                    Entry e;
                    if (!PendingEntries.TryDequeue(out e))
                    {
                        break;
                    }

                    if (e.IsWatch)
                    {
                        this.watchees.Add(e);
                    }
                    else
                    {
                        this.watchees.Remove(e);
                    }
                }
            }

            void NotifyWatchees()
            {
                List<Entry> watcheeList = this.watchees;
                for (int i = 0; i < watcheeList.Count;)
                {
                    Entry e = watcheeList[i];
                    if (!e.Thread.IsAlive)
                    {
                        watcheeList.RemoveAt(i);
                        try
                        {
                            e.Task();
                        }
                        catch (Exception t)
                        {
                            Log.Warn("Thread death watcher task raised an exception:", t);
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        sealed class Entry
        {
            internal readonly Thread Thread;
            internal readonly Action Task;
            internal readonly bool IsWatch;

            public Entry(Thread thread, Action task, bool isWatch)
            {
                this.Thread = thread;
                this.Task = task;
                this.IsWatch = isWatch;
            }

            public override int GetHashCode() => this.Thread.GetHashCode() ^ this.Task.GetHashCode();

            public override bool Equals(object obj)
            {
                if (obj == this)
                {
                    return true;
                }

                if (!(obj is Entry))
                {
                    return false;
                }

                var that = (Entry)obj;
                return this.Thread == that.Thread && this.Task == that.Task;
            }
        }
    }
}
