// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using NetUV.Core.Common;

    sealed class BufferQueue : IDisposable
    {
        readonly SpscLinkedQueue<BufferRef> queue;
        bool disposed;

        internal BufferQueue()
        {
            this.queue = new SpscLinkedQueue<BufferRef>();
            this.disposed = false;
        }

        internal void Enqueue(BufferRef bufferRef)
        {
            Contract.Requires(bufferRef != null);

            this.CheckDisposed();
            this.queue.Offer(bufferRef);
        }

        internal bool TryDequeue(out BufferRef bufferRef)
        {
            this.CheckDisposed();
            bufferRef = this.queue.Poll();
            return bufferRef != null;
        }

        void CheckDisposed()
        {
            if (Volatile.Read(ref this.disposed))
            {
                throw new ObjectDisposedException(nameof(BufferQueue));
            }
        }

        internal void Clear() => this.queue.Clear();

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            Volatile.Write(ref this.disposed, true);
            this.Clear();
        }
    }
}
