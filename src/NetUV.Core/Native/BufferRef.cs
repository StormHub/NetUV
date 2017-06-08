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
        readonly int index;
        readonly int length;

        IArrayBuffer<byte> buffer;
        GCHandle array;
        GCHandle handle;

        internal BufferRef(IArrayBuffer<byte> buffer, int index, int length)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index > 0 && length > 0);

            this.buffer = buffer;
            this.index = buffer.ArrayOffset + index;
            this.length = length;
        }

        internal uv_buf_t[] GetBuffer()
        {
            if (this.buffer == null)
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

            IArrayBuffer<byte> arrayBufer = this.buffer;
            this.array = GCHandle.Alloc(arrayBufer.Array, GCHandleType.Pinned);
            IntPtr arrayHandle = this.array.AddrOfPinnedObject();

            var bufs = new[] { new uv_buf_t(arrayHandle + this.index, this.length) };
            this.handle = GCHandle.Alloc(bufs, GCHandleType.Pinned);

            return bufs;
        }

        internal IArrayBuffer<byte> GetByteBuffer()
        {
            if (this.buffer == null)
            {
                throw new ObjectDisposedException($"{nameof(BufferRef)} has already been disposed.");
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
            this.buffer = null;
        }
    }
}
