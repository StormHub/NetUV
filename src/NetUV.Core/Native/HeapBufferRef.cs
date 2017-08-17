// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Buffers;

    sealed class HeapBufferRef : IDisposable
    {
        readonly IByteBuffer heapBuffer;
        readonly PooledHeapByteBuffer buffer;
        readonly uv_buf_t[] bufs;

        GCHandle array;
        GCHandle handle;

        internal HeapBufferRef(IByteBuffer buffer)
        {
            Contract.Requires(buffer != null);

            // For direct IO buffers, unwrap if the buffer resource leak
            // detection is enabled
            if (buffer is WrappedByteBuffer)
            {
                this.buffer = (PooledHeapByteBuffer)buffer.Unwrap();
            }
            else
            {
                this.buffer = (PooledHeapByteBuffer)buffer;
            }

            this.heapBuffer = buffer;
            this.bufs = new uv_buf_t[1];
        }

        internal uv_buf_t[] GetNativeBuffer()
        {
            Debug.Assert(!this.array.IsAllocated && !this.handle.IsAllocated);

            this.array = GCHandle.Alloc(this.buffer.Array, GCHandleType.Pinned);
            IntPtr arrayHandle = this.array.AddrOfPinnedObject();

            int offset = this.buffer.ArrayOffset + this.buffer.WriterIndex;
            int length = this.buffer.WritableBytes;

            this.bufs[0] = new uv_buf_t(arrayHandle + offset, length);
            this.handle = GCHandle.Alloc(this.bufs, GCHandleType.Pinned);

            return this.bufs;
        }

        internal IByteBuffer GetHeapBuffer() => this.heapBuffer;

        public void Dispose()
        {
            if (this.array.IsAllocated)
            {
                this.array.Free();
            }

            if (this.handle.IsAllocated)
            {
                this.handle.Free();
            }
        }
    }
}
