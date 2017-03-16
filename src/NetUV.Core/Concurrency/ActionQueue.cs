// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Concurrency
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using System.Threading.Tasks;

    sealed class ActionQueue : IDisposable
    {
        readonly ConcurrentQueue<Activator> queue;
        readonly Gate gate;
        volatile bool disposed;

        class Activator
        {
            readonly Action<object> action;
            readonly object state;

            internal Activator(Action<object> action, object state)
            {
                Contract.Requires(action != null);
                this.action = action;
                this.state = state;
            }

            internal void Invoke() => this.action.Invoke(this.state);
        }

        internal ActionQueue()
        {
            this.queue = new ConcurrentQueue<Activator>();
            this.gate = new Gate();
            this.disposed = false;
        }

        internal void Enqueue(Action<object> action, object state)
        {
            Contract.Requires(action != null);

            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(ActionQueue));
            }

            var activator = new Activator(action, state);
            this.queue.Enqueue(activator);
            Task.Run(() => this.Next()).Ignore();
        }

        void Next()
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
                            activator.Invoke();
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

                while (this.queue.TryDequeue(out Activator ignore)) { }
            }
        }
    }
}
