// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Buffers;

    sealed class BufferRef : IDisposable
    {
        readonly int count;
        readonly bool retain;

        ByteBuffer buffer;
        GCHandle array;
        GCHandle handle;

        internal BufferRef(WritableBuffer writableBuffer)
        {
            Contract.Requires(writableBuffer.InternalBuffer != null);

            this.buffer = writableBuffer.InternalBuffer;
            this.retain = true;
            this.count = writableBuffer.Index;
        }

        internal BufferRef(ByteBuffer buffer, bool retain = true)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(buffer.ArrayBuffer != null);

            this.buffer = buffer;
            this.count = buffer.Count;
            this.retain = retain;
        }

        internal uv_buf_t[] GetBuffer()
        {
            if (this.buffer?.ArrayBuffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(BufferRef)} has already been disposed.");
            }

            if (this.handle.IsAllocated 
                || this.array.IsAllocated)
            {
                throw new InvalidOperationException(
                    $"{nameof(BufferRef)} has already been initialized and not released yet.");
            }

            IArrayBuffer<byte> arrayBuffer = this.buffer.ArrayBuffer;
            Contract.Assert(arrayBuffer.Array != null);

            this.array = GCHandle.Alloc(arrayBuffer.Array, GCHandleType.Pinned);
            IntPtr arrayHandle = this.array.AddrOfPinnedObject();

            var bufs = new[] { new uv_buf_t(arrayHandle + arrayBuffer.Offset, this.count) };
            this.handle = GCHandle.Alloc(bufs, GCHandleType.Pinned);

            return bufs;
        }

        internal ByteBuffer GetByteBuffer()
        {
            if (this.buffer == null)
            {
                throw new ObjectDisposedException(
                    $"{nameof(BufferRef)} has already been disposed.");
            }

            this.Release();
            return this.buffer;
        }

        void Release()
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

        public void Dispose()
        {
            this.Release();

            if (!this.retain)
            {
                this.buffer.Dispose();
            }

            this.buffer = null;
        }
    }
}
