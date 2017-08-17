// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;

    sealed class HeapBufferQueue : IDisposable
    {
        readonly SpscLinkedQueue<HeapBufferRef> queue;
        volatile bool disposed;

        internal HeapBufferQueue()
        {
            this.queue = new SpscLinkedQueue<HeapBufferRef>();
            this.disposed = false;
        }

        internal void Enqueue(HeapBufferRef bufferRef)
        {
            Contract.Requires(bufferRef != null);

            this.CheckDisposed();
            this.queue.Offer(bufferRef);
        }

        internal bool TryDequeue(out HeapBufferRef bufferRef)
        {
            this.CheckDisposed();
            bufferRef = this.queue.Poll();
            return bufferRef != null;
        }

        void CheckDisposed()
        {
            if (this.disposed)
            {
                ThrowObjectDisposedException();
            }
        }

        static void ThrowObjectDisposedException() => throw new ObjectDisposedException(nameof(HeapBufferQueue));

        internal void Clear() => this.queue.Clear();

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Clear();
        }
    }
}
