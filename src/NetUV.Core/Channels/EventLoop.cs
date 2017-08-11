// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using NetUV.Core.Handles;

    public sealed class EventLoop : IDisposable
    {
        readonly TaskCompletionSource<bool> loopCompletionSource;
        readonly Thread thread;
        readonly Loop loop;
        readonly Async asyncHandle;
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
            this.asyncHandle = this.loop.CreateAsync(this.OnAsync);
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
#pragma warning disable 420
                if (Interlocked.CompareExchange(ref this.executionState, newState, oldState) == oldState)
#pragma warning restore 420
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

        void OnAsync(Async handle)
        {
            while (true)
            {
                if (this.IsShuttingDown)
                {
                    this.asyncHandle.RemoveReference();
                    this.loop.Dispose();
                    break;
                }

                if (!this.queue.TryDequeue(out Activator activator))
                {
                    break;
                }

                activator.Execute(this.loop);
            }
        }

        public void Dispose()
        {
#pragma warning disable 168
            while (this.queue.TryDequeue(out Activator _))
#pragma warning restore 168
            { }

            this.loop.Dispose();
        } 
    }
}
