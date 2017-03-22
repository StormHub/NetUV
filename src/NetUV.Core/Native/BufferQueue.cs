// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using System.Threading;

    sealed class BufferQueue : IDisposable
    {
        readonly ConcurrentQueue<BufferRef> queue;
        bool disposed;

        internal BufferQueue()
        {
            this.queue = new ConcurrentQueue<BufferRef>();
            this.disposed = false;
        }

        internal void Enqueue(BufferRef bufferRef)
        {
            Contract.Requires(bufferRef != null);

            this.CheckDisposed();
            this.queue.Enqueue(bufferRef);
        }

        internal bool TryDequeue(out BufferRef bufferRef)
        {
            this.CheckDisposed();
            return this.queue.TryDequeue(out bufferRef);
        }

        void CheckDisposed()
        {
            if (Volatile.Read(ref this.disposed))
            {
                throw new ObjectDisposedException(nameof(BufferQueue));
            }
        }

        internal void Clear()
        {
            while (this.queue.TryDequeue(out BufferRef bufferRef))
            {
                bufferRef.Dispose();
            }
        }

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
