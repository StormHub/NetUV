// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWhenPossible
#pragma warning disable 420
namespace NetUV.Core.Channels
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using NetUV.Core.Handles;
    using NetUV.Core.Logging;

    using Timer = NetUV.Core.Handles.Timer;

    public sealed class EventLoop
    {
        const int DefaultBreakoutTime = 100; //ms
        static readonly TimeSpan DefaultShutdownQuietPeriod = TimeSpan.FromSeconds(2);
        static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);
        static readonly TimeSpan DefaultBreakoutInterval = TimeSpan.FromMilliseconds(DefaultBreakoutTime);

        static readonly ILog Logger = LogFactory.ForContext<EventLoop>();

        const int NotStartedState = 1;
        const int StartedState = 2;
        const int ShuttingDownState = 3;
        const int ShutdownState = 4;
        const int TerminatedState = 5;

        readonly long preciseBreakoutInterval;
        readonly ConcurrentQueue<Activator> taskQueue;
        readonly Thread thread;
        readonly TaskCompletionSource<bool> terminationCompletionSource;
        readonly Loop loop;
        readonly Async asyncHandle;
        readonly Timer timerHandle;

        volatile int executionState = NotStartedState;

        long lastExecutionTime;
        long gracefulShutdownStartTime;
        long gracefulShutdownQuietPeriod;
        long gracefulShutdownTimeout;

        // Flag to indicate whether async handle should be used to wake up 
        // the loop, only accessed when InEventLoop is true
        bool wakeUp = true;

        sealed class Activator
        {
            readonly Action<object> activator;
            readonly object state;
            readonly TaskCompletionSource<bool> completion;

            internal Activator(Action activator)
            {
                Debug.Assert(activator != null);

                this.activator = state => activator();
                this.state = null;
                this.completion = new TaskCompletionSource<bool>();
            }

            internal Activator(Action<object> activator, object state)
            {
                Debug.Assert(activator != null);

                this.activator = activator;
                this.state = state;
                this.completion = new TaskCompletionSource<bool>();
            }

            internal void Execute()
            {
                try
                {
                    this.activator(this.state);
                    this.completion.TrySetResult(true);
                }
                catch (Exception exception)
                {
                    this.completion.TrySetException(exception);
                }
            }

            internal Task Completion => this.completion.Task;
        }

        public EventLoop() : this(DefaultBreakoutInterval)
        {
        }

        EventLoop(TimeSpan breakoutInterval)
        {
            this.preciseBreakoutInterval = (long)breakoutInterval.TotalMilliseconds;
            this.terminationCompletionSource = new TaskCompletionSource<bool>();
            this.taskQueue = new ConcurrentQueue<Activator>();
            this.loop = new Loop();
            this.asyncHandle = this.loop.CreateAsync(this.OnCallback);
            this.timerHandle = this.loop.CreateTimer();
            this.thread = new Thread(Run);
            this.thread.Start(this);
        }

        public Loop Loop => this.loop;

        static void Run(object state)
        {
            var eventLoop = (EventLoop)state;
            eventLoop.StartLoop();
        }

        void OnCallback(ScheduleHandle handle)
        {
            if (this.IsShuttingDown)
            {
                this.ShuttingDown();
            }
            else
            {
                this.RunAllTasks(this.preciseBreakoutInterval);
            }
        }

        void StartLoop()
        {
            try
            {
                this.UpdateLastExecutionTime();
                if (Interlocked.CompareExchange(ref this.executionState, StartedState, NotStartedState) != NotStartedState)
                {
                    throw new InvalidOperationException($"Invalid {nameof(EventLoop)} state {this.executionState}");
                }
                this.loop.RunDefault();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Loop run default error. {0}", ex);
                this.terminationCompletionSource.TrySetException(ex);
            }
            finally
            {
                Logger.Info("Loop thread finished.");
                this.CleanupAndTerminate();
            }
        }

        void StopLoop()
        {
            try
            {
                // Drop out from the loop so that it can be safely disposed,
                // other active handles will be closed by loop.Close()
                this.timerHandle.Stop();
                this.loop.Stop();
            }
            catch (Exception ex)
            {
                Logger.Error("{}: shutting down loop error", ex);
            }
        }

        void ShuttingDown()
        {
            if (this.gracefulShutdownStartTime == 0)
            {
                this.gracefulShutdownStartTime = this.GetLoopTime();
            }

            bool runTask;
            do
            {
                runTask = this.RunAllTasks();

                // Terminate if the quiet period is 0.
                if (this.gracefulShutdownQuietPeriod == 0)
                {
                    this.StopLoop();
                    return;
                }
            }
            while (runTask);

            long nanoTime = this.GetLoopTime();

            // Shutdown timed out
            if (nanoTime - this.gracefulShutdownStartTime <= this.gracefulShutdownTimeout
                && nanoTime - this.lastExecutionTime <= this.gracefulShutdownQuietPeriod)
            {
                // Wait for quiet period passed
                this.timerHandle.Start(this.OnCallback, DefaultBreakoutTime, 0); // 100ms
            }
            else
            {
                // No tasks were added for last quiet period
                this.StopLoop();
            }
        }

        void CleanupAndTerminate()
        {
            try
            {
                this.Cleanup();
            }
            finally
            {
                Interlocked.Exchange(ref this.executionState, TerminatedState);
                if (!this.taskQueue.IsEmpty)
                {
                    Logger.Warn($"{nameof(EventLoop)} terminated with non-empty task queue ({this.taskQueue.Count})");
                }
                this.terminationCompletionSource.TrySetResult(true);
            }
        }

        void Cleanup()
        {
            SafeDispose(this.timerHandle);
            SafeDispose(this.asyncHandle);
            SafeDispose(this.loop);

            while (!this.taskQueue.IsEmpty)
            {
                this.taskQueue.TryDequeue(out Activator _);
            }

            Logger.Info($"{nameof(EventLoop)} disposed.");
        }

        static void SafeDispose(IDisposable handle)
        {
            try
            {
                Logger.InfoFormat("Disposing {0}", handle.GetType());
                handle.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WarnFormat("{0} dispose error {1}", handle.GetType(), ex);
            }
        }


        void UpdateLastExecutionTime() => this.lastExecutionTime = this.GetLoopTime();

        long GetLoopTime()
        {
            this.loop.UpdateTime();
            return this.loop.Now;
        }

        void RunAllTasks(long timeout)
        {
            Activator task = this.PollTask();
            if (task == null)
            {
                this.AfterRunningAllTasks();
                return;
            }

            long start = this.GetLoopTime();
            long runTasks = 0;
            long executionTime;
            this.wakeUp = false;
            for (; ;)
            {
                SafeExecute(task);

                runTasks++;

                // Check timeout every 64 tasks because nanoTime() is relatively expensive.
                // XXX: Hard-coded value - will make it configurable if it is really a problem.
                if ((runTasks & 0x3F) == 0)
                {
                    executionTime = this.GetLoopTime();
                    if ((executionTime - start) >= timeout)
                    {
                        break;
                    }
                }

                task = this.PollTask();
                if (task == null)
                {
                    executionTime = this.GetLoopTime();
                    break;
                }
            }
            this.wakeUp = true;

            this.AfterRunningAllTasks();
            this.lastExecutionTime = executionTime;
        }

        void AfterRunningAllTasks()
        {
            if (this.IsShuttingDown)
            {
                // Immediate shutdown
                this.WakeUp(true);
                return;
            }

            if (!this.taskQueue.IsEmpty)
            {
                this.timerHandle.Start(this.OnCallback, DefaultBreakoutTime, 0);
            }
        }

        Activator PollTask() => PollTaskFrom(this.taskQueue);

        bool RunAllTasks()
        {
            bool runTask;
            bool ranAtLeastOne = false;
            while (true)
            {
                runTask = RunAllTasksFrom(this.taskQueue);
                if (!runTask)
                {
                    break;
                }
                else
                {
                    ranAtLeastOne = true;
                }
            }
            if (ranAtLeastOne)
            {
                this.lastExecutionTime = this.GetLoopTime();
            }
            return ranAtLeastOne;
        }

        static bool RunAllTasksFrom(ConcurrentQueue<Activator> taskQueue)
        {
            Activator task = PollTaskFrom(taskQueue);
            if (task == null)
            {
                return false;
            }
            for (; ;)
            {
                SafeExecute(task);
                task = PollTaskFrom(taskQueue);
                if (task == null)
                {
                    return true;
                }
            }
        }
        static void SafeExecute(Activator task)
        {
            try
            {
                task.Execute();
            }
            catch (Exception ex)
            {
                Logger.WarnFormat("A task raised an exception. Task: {0}", ex);
            }
        }
        static Activator PollTaskFrom(ConcurrentQueue<Activator> taskQueue) =>
            taskQueue.TryDequeue(out Activator task) ? task : null;

        public Task TerminationCompletion => this.terminationCompletionSource.Task;

        public bool IsShuttingDown => this.executionState >= ShuttingDownState;

        public bool IsShutdown => this.executionState >= ShutdownState;

        public bool IsTerminated => this.executionState == TerminatedState;

        public bool InEventLoop => this.thread == Thread.CurrentThread;

        void WakeUp(bool inEventLoop)
        {
            // If the executor is not in the event loop, wake up the loop by async handle immediately.
            //
            // If the executor is in the event loop and in the middle of RunAllTasks, no need to 
            // wake up the loop again because this is normally called by the current running task.
            if (!inEventLoop || this.wakeUp)
            {
                this.asyncHandle.Send();
            }
        }

        public Task ExecuteAsync(Action action)
        {
            Contract.Requires(action != null);

            var task = new Activator(action);
            this.Execute(task);
            return task.Completion;
        }

        public Task ExecuteAsync(Action<object> action, object state)
        {
            Contract.Requires(action != null);

            var task = new Activator(action, state);
            this.Execute(task);
            return task.Completion;
        }

        void Execute(Activator task)
        {
            bool inEventLoop = this.InEventLoop;
            if (inEventLoop)
            {
                this.AddTask(task);
            }
            else
            {
                this.AddTask(task);
                if (this.IsShutdown)
                {
                    Reject($"{nameof(EventLoop)} terminated");
                }
            }
            this.WakeUp(inEventLoop);
        }

        void AddTask(Activator task)
        {
            if (this.IsShutdown)
            {
                Reject($"{nameof(EventLoop)} already shutdown");
            }
            this.taskQueue.Enqueue(task);
        }

        static void Reject(string message) => throw new InvalidOperationException(message);

        public Task ShutdownGracefullyAsync() => this.ShutdownGracefullyAsync(DefaultShutdownQuietPeriod, DefaultShutdownTimeout);

        public Task ShutdownGracefullyAsync(TimeSpan quietPeriod, TimeSpan timeout)
        {
            Contract.Requires(quietPeriod >= TimeSpan.Zero);
            Contract.Requires(timeout >= quietPeriod);

            if (this.IsShuttingDown)
            {
                return this.TerminationCompletion;
            }

            bool inEventLoop = this.InEventLoop;
            bool wakeUpLoop;
            int oldState;
            for (; ;)
            {
                if (this.IsShuttingDown)
                {
                    return this.TerminationCompletion;
                }
                int newState;
                wakeUpLoop = true;
                oldState = this.executionState;
                if (inEventLoop)
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
                            wakeUpLoop = false;
                            break;
                    }
                }
                if (Interlocked.CompareExchange(ref this.executionState, newState, oldState) == oldState)
                {
                    break;
                }
            }

            this.gracefulShutdownQuietPeriod = (long)quietPeriod.TotalMilliseconds;
            this.gracefulShutdownTimeout = (long)timeout.TotalMilliseconds;

            if (oldState == NotStartedState)
            {
                // If the loop is not yet running, close all handles directly
                // because wake up callback will not be executed.
                this.CleanupAndTerminate();
            }
            else
            {
                if (wakeUpLoop)
                {
                    this.WakeUp(inEventLoop);
                }
            }

            return this.TerminationCompletion;
        }
    }
}
