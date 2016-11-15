// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Concurrency
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using NetUV.Core.Logging;

    sealed class AsyncLock
    {
        static readonly ILog Log = LogFactory.ForContext<AsyncLock>();

        readonly SemaphoreSlim semaphore;
        readonly TaskScheduler scheduler;

        public AsyncLock() : this(TaskScheduler.Default)
        { }

        public AsyncLock(TaskScheduler scheduler)
        {
            Contract.Requires(scheduler != null);

            this.scheduler = scheduler;
            this.semaphore = new SemaphoreSlim(1);
        }

        public Task<IDisposable> LockAsync()
        {
            Task wait = this.semaphore.WaitAsync();
            if (wait.IsCompleted)
            {
                return Task.FromResult((IDisposable)new LockReleaser(this));
            }
            else
            {
                return wait.ContinueWith(
                    _ => (IDisposable)new LockReleaser(this),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, 
                    this.scheduler);
            }
        }

        class LockReleaser : IDisposable
        {
            AsyncLock target;

            internal LockReleaser(AsyncLock target)
            {
                this.target = target;
            }

            public void Dispose()
            {
                if (this.target == null)
                {
                    return;
                }

                AsyncLock tmp = this.target;
                this.target = null;
                try
                {
                    tmp.semaphore.Release();
                }
                catch (Exception exception)
                {
                    Log.Warn($"{nameof(AsyncLock)} dispose error.", exception);
                }
            }
        }
    }
}
