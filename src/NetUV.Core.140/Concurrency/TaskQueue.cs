// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Concurrency
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using System.Threading.Tasks;

    sealed class TaskQueue : IDisposable
    {
        readonly ConcurrentQueue<Activator> queue;
        readonly Gate gate;
        volatile bool disposed;

        class Activator
        {
            readonly Func<object, Task> activator;
            readonly object state;
            readonly TaskCompletionSource<bool> completion;

            internal Activator(Func<object, Task> activator, object state)
            {
                Contract.Requires(activator != null);

                this.activator = activator;
                this.state = state;
                this.completion = new TaskCompletionSource<bool>();
            }

            internal async Task ExecuteAsync()
            {
                try
                {
                    await this.activator(this.state);
                    this.completion.TrySetResult(true);
                }
                catch (Exception exception)
                {
                    this.completion.TrySetException(exception);
                }
            }

            internal Task Completion => this.completion.Task;
        }

        internal TaskQueue()
        {
            this.gate = new Gate();
            this.disposed = false;
            this.queue = new ConcurrentQueue<Activator>();
        }

        internal Task Enqueue(Func<object, Task> activator, object state)
        {
            Contract.Requires(activator != null);
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(TaskQueue));
            }

            var taskCompletion = new Activator(activator, state);
            this.queue.Enqueue(taskCompletion);
            this.Next().Ignore();

            return taskCompletion.Completion;
        }

        async Task Next()
        {
            while (!this.queue.IsEmpty)
            {
                IDisposable aquired = null;
                try
                {
                    if (this.disposed)
                    {
                        return;
                    }

                    aquired = this.gate.TryAquire();
                    if (aquired == null)
                    {
                        return;
                    }

                    while (!this.queue.IsEmpty)
                    {
                        if (this.queue.TryDequeue(out Activator activator))
                        {
                            await activator.ExecuteAsync();
                        }
                    }
                }
                finally
                {
                    aquired?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            using (this.gate.Aquire())
            {
                this.disposed = true;

#pragma warning disable 168
                while (this.queue.TryDequeue(out Activator _)) { }
#pragma warning restore 168
            }
        }
    }
}
