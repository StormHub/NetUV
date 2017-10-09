// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable 420
namespace NetUV.Core.Channels
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using NetUV.Core.Handles;
    using Timer = NetUV.Core.Handles.Timer;

    public sealed class EventLoop : IDisposable
    {
        const long DefaultBreakoutInterval = 100;
        readonly TaskCompletionSource<bool> loopCompletionSource;
        readonly Thread thread;
        readonly Loop loop;
        readonly Async asyncHandle;
        readonly Timer timerHandle;
        readonly ConcurrentQueue<Activator> queue;

        const int NotStartedState = 1;
        const int StartedState = 2;
        const int ShuttingDownState = 3;
        const int ShutdownState = 4;
        const int TerminatedState = 5;

        volatile int executionState = NotStartedState;

        class Activator
        {
            readonly Action<Loop, object> activator;
            readonly object state;
            readonly TaskCompletionSource<bool> completion;

            internal Activator(Action<Loop> activator)
            {
                this.activator = (loop, state) => activator(loop);
                this.state = null;
                this.completion = new TaskCompletionSource<bool>();
            }

            internal Activator(Action<Loop, object> activator, object state)
            {
                Contract.Requires(activator != null);

                this.activator = activator;
                this.state = state;
                this.completion = new TaskCompletionSource<bool>();
            }

            internal void Execute(Loop loop)
            {
                try
                {
                    this.activator(loop, this.state);
                    this.completion.TrySetResult(true);
                }
                catch (Exception exception)
                {
                    this.completion.TrySetException(exception);
                }
            }

            internal Task Completion => this.completion.Task;
        }

        public EventLoop()
        {
            this.loopCompletionSource = new TaskCompletionSource<bool>();
            this.queue = new ConcurrentQueue<Activator>();
            this.loop = new Loop();
            this.asyncHandle = this.loop.CreateAsync(this.OnCallback);
            this.timerHandle = this.loop.CreateTimer();
            this.thread = new Thread(RunLoop);
            this.thread.Start(this);
        }

        bool IsShuttingDown => this.executionState >= ShuttingDownState;

        public bool IsShutdown => this.executionState >= ShutdownState;

        public bool IsTerminated => this.executionState == TerminatedState;

        public void ScheduleStop()
        {
            if (this.IsShuttingDown)
            {
                return;
            }

            bool wakeup;
            while (true)
            {
                if (this.IsShuttingDown)
                {
                    return;
                }

                int newState;
                wakeup = true;
                int oldState = this.executionState;
                if (Thread.CurrentThread == this.thread)
                {
                    newState = ShuttingDownState;
                }
                else
                {
                    switch (oldState)
                    {
                        case NotStartedState:
                        case StartedState:
                            newState = ShuttingDownState;
                            break;
                        default:
                            newState = oldState;
                            wakeup = false;
                            break;
                    }
                }
                if (Interlocked.CompareExchange(ref this.executionState, newState, oldState) == oldState)
                {
                    break;
                }
            }

            if (wakeup)
            {
                this.asyncHandle.Send();
            }
        }

        public Task Schedule(Action<Loop, object> action, object state)
        {
            if (this.executionState != StartedState)
            {
                throw new InvalidOperationException($"{nameof(EventLoop)} is not in started state.");
            }

            var activator = new Activator(action, state);
            this.queue.Enqueue(activator);
            this.asyncHandle.Send();

            return activator.Completion;
        }

        public Task Schedule(Action<Loop> action)
        {
            if (this.executionState != StartedState)
            {
                throw new InvalidOperationException($"{nameof(EventLoop)} is not in started state.");
            }

            var activator = new Activator(action);
            this.queue.Enqueue(activator);
            this.asyncHandle.Send();

            return activator.Completion;
        }

        public Task LoopCompletion => this.loopCompletionSource.Task;

        static void RunLoop(object state)
        {
            var eventLoop = (EventLoop)state;

            try
            {
                eventLoop.executionState = StartedState;
                eventLoop.loop.RunDefault();
                eventLoop.loopCompletionSource.TrySetResult(true);
            }
            catch (Exception exception)
            {
                eventLoop.loopCompletionSource.TrySetException(exception);
            }

            eventLoop.executionState = TerminatedState;
        }

        void OnCallback(ScheduleHandle handle)
        {
            long runTasks = 0;
            long deadline = this.loop.NowInHighResolution + DefaultBreakoutInterval;
            while (true)
            {
                if (this.IsShuttingDown)
                {
                    this.Close();
                    break;
                }

                if (!this.queue.TryDequeue(out Activator activator))
                {
                    this.timerHandle.Stop();
                    break;
                }

                activator.Execute(this.loop);
                runTasks++;
                if ((runTasks & 0x3F) == 0)
                {
                    long executionTime = this.loop.NowInHighResolution;
                    if (executionTime >= deadline)
                    {
                        this.timerHandle.Start(this.OnCallback, DefaultBreakoutInterval, 1);
                        break;
                    }
                }
            }
        }

        void Close()
        {
            if (this.IsShutdown)
            {
                return;
            }

            this.timerHandle.Stop();
            this.asyncHandle.RemoveReference();
            this.timerHandle.RemoveReference();
            this.loop.Dispose();
        }

        public void Dispose()
        {
            while (this.queue.TryDequeue(out Activator _))
            { }

            this.loop.Dispose();
        } 
    }
}
