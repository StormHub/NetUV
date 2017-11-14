// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using NetUV.Core.Buffers;
    using NetUV.Core.Common;

    sealed class PendingReadQueue : IDisposable
    {
        readonly SpscLinkedQueue<ReadBufferRef> queue;
        volatile bool disposed;

        internal PendingReadQueue()
        {
            this.queue = new SpscLinkedQueue<ReadBufferRef>();
            this.disposed = false;
        }

        internal void Enqueue(ReadBufferRef bufferRef)
        {
            Contract.Requires(bufferRef != null);

            this.CheckDisposed();
            this.queue.Offer(bufferRef);
        }

        internal bool TryDequeue(out ReadBufferRef bufferRef)
        {
            this.CheckDisposed();
            bufferRef = this.queue.Poll();
            return bufferRef != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckDisposed()
        {
            if (this.disposed)
            {
                ThrowHelper.ThrowObjectDisposedException(nameof(PendingReadQueue));
            }
        }

        internal void Clear() => this.queue.Clear();

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Clear();
            }
            this.disposed = true;
        }
    }
}
