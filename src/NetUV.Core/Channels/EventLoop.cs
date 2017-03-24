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
        readonly ConcurrentQueue<Activator> queue;
        readonly Thread thread;
        Loop loop;
        long loopState;

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
            this.queue = new ConcurrentQueue<Activator>();
            this.loopState = 0;
            this.thread = new Thread(this.RunLoop);
        }

        public void ScheduleStop() => Interlocked.CompareExchange(ref this.loopState, 1, 0);

        public Task Schedule(Action<Loop, object> action, object state)
        {
            if (Interlocked.Read(ref this.loopState) > 0)
            {
                throw new ObjectDisposedException($"{nameof(EventLoop)}");
            }

            var activator = new Activator(action, state);
            this.queue.Enqueue(activator);

            return activator.Completion;
        }

        public Task Schedule(Action<Loop> action)
        {
            if (Interlocked.Read(ref this.loopState) > 0)
            {
                throw new ObjectDisposedException($"{nameof(EventLoop)}");
            }

            var activator = new Activator(action);
            this.queue.Enqueue(activator);

            return activator.Completion;
        }

        public Task RunAsync()
        {
            if (this.thread.ThreadState != ThreadState.Unstarted)
            {
                throw new InvalidOperationException(
                    $"{nameof(EventLoop)} invalid thread state {this.thread.ThreadState}.");
            }

            var completion = new TaskCompletionSource<bool>();
            this.thread.Start(completion);
            return completion.Task;
        }

        void RunLoop(object state)
        {
            var completion = (TaskCompletionSource<bool>)state;

            try
            {
                this.loop = new Loop();
                this.loop
                    .CreateIdle()
                    .Start(this.OnIdle);

                this.loop.RunDefault();
                completion.TrySetResult(true);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }

        void OnIdle(Idle handle)
        {
            if (this.loopState > 0)
            {
                handle.CloseHandle(this.OnClosed);
                return;
            }

            if (this.queue.TryDequeue(out Activator activator))
            {
                activator.Execute(this.loop);
            }
        }

        void OnClosed(Idle handle)
        {
            handle.Dispose();
            this.loop?.Dispose();
            this.loop = null;
        }

        public void Dispose()
        {
#pragma warning disable 168
            while (this.queue.TryDequeue(out Activator _))
#pragma warning restore 168
            { }

            this.loop?.Dispose();
            this.loop = null;
        } 
    }
}
