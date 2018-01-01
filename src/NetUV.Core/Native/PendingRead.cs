// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace NetUV.Core.Native
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using NetUV.Core.Buffers;

    sealed class PendingRead : IDisposable
    {
        IByteBuffer buffer;
        GCHandle pin;

        internal PendingRead()
        {
            this.Reset();
        }

        internal IByteBuffer Buffer => this.buffer;

        internal uv_buf_t GetBuffer(IByteBuffer buf)
        {
            Debug.Assert(!this.pin.IsAllocated);

            // Do not pin the buffer again if it is already pinned
            IntPtr arrayHandle = buf.AddressOfPinnedMemory();
            int index = buf.WriterIndex;

            if (arrayHandle == IntPtr.Zero)
            {
                this.pin = GCHandle.Alloc(buf.Array, GCHandleType.Pinned);
                arrayHandle = this.pin.AddrOfPinnedObject();
                index += buf.ArrayOffset;
            }
            int length = buf.WritableBytes;
            this.buffer = buf;

            return new uv_buf_t(arrayHandle + index, length);
        }

        internal void Reset()
        {
            this.buffer = Unpooled.Empty;
        }

        void Release()
        {
            if (this.pin.IsAllocated)
            {
                this.pin.Free();
            }
        }

        public void Dispose()
        {
            this.Release();
            this.Reset();
        }
    }
}
