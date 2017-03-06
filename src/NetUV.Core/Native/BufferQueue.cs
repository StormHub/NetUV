// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Common;
    using NetUV.Core.Concurrency;

    sealed class BufferQueue : IDisposable
    {
        readonly Deque<BufferRef> queue;
        readonly Gate gate;
        volatile bool disposed;

        internal BufferQueue()
        {
            this.queue = new Deque<BufferRef>();
            this.gate = new Gate();
            this.disposed = false;
        }

        internal int Count => this.queue.Count;

        internal void Enqueue(BufferRef bufferRef)
        {
            Contract.Requires(bufferRef != null);

            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(BufferQueue));
                }

                this.queue.AddToFront(bufferRef);
            }
        }

        internal bool TryDequeue(out BufferRef bufferRef)
        {
            bufferRef = null;

            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(BufferQueue));
                }

                if (this.queue.Count > 0)
                {
                    bufferRef = this.queue.RemoveFromBack();
                }
            }

            return bufferRef != null;
        }

        internal void Clear()
        {
            using (this.gate.Aquire())
            {
                while (this.queue.Count > 0)
                {
                    BufferRef bufferRef = this.queue.RemoveFromBack();
                    bufferRef.Dispose();
                }
            }
        }

        public void Dispose()
        {
            this.disposed = true;
            this.Clear();
        }
    }
}
