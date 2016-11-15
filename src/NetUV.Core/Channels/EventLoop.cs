// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NetUV.Core.Concurrency;
    using NetUV.Core.Handles;

    public sealed class EventLoop : IDisposable
    {
        readonly Thread thread;
        readonly TaskQueue pendingTaskQueue;

        public EventLoop()
        {
            this.Handle = new Loop();
            this.thread = new Thread(this.RunLoop);
            this.pendingTaskQueue = new TaskQueue();
        }

        public Loop Handle { get; }

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
                this.Handle.RunDefault();
                completion.TrySetResult(true);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }

        public void Dispose()
        {
            this.pendingTaskQueue.Dispose();
            this.Handle.Dispose();
        } 
    }
}
