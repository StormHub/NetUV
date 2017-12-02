// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using NetUV.Core.Buffers;

    sealed class ReadBufferRef : IDisposable
    {
        readonly IByteBuffer buffer;
        uv_buf_t buf;
        GCHandle pin;

        internal ReadBufferRef(IByteBuffer buffer)
        {
            Debug.Assert(buffer != null);
            this.buffer = buffer;

            IByteBuffer byteBuffer = this.buffer;
            int index = byteBuffer.WriterIndex;
            int length = byteBuffer.WritableBytes;

            IntPtr addr = byteBuffer.AddressOfPinnedMemory();
            if (addr != IntPtr.Zero)
            {
                this.buf = new uv_buf_t(addr + index, length);
            }
            else
            {
                byte[] array = byteBuffer.Array;
                this.pin = GCHandle.Alloc(array, GCHandleType.Pinned);
                addr = this.pin.AddrOfPinnedObject();
                this.buf = new uv_buf_t(addr + buffer.ArrayOffset + index, length);
            }
        }

        public IByteBuffer Buffer => this.buffer;

        public ref uv_buf_t Buf => ref this.buf;

        public void Dispose()
        {
            if (this.pin.IsAllocated)
            {
                this.pin.Free();
            }
        }
    }
}
