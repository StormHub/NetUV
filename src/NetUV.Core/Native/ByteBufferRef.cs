// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Buffers;

    sealed class ByteBufferRef : IDisposable
    {
        readonly int index;
        readonly int length;

        readonly IByteBuffer buffer;
        readonly List<GCHandle> handles;

        internal ByteBufferRef(IByteBuffer buffer, int index, int length)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index > 0 && length > 0);

            this.buffer = (IByteBuffer)buffer.Retain();
            this.index = buffer.ArrayOffset + index;
            this.length = length;
            this.handles = new List<GCHandle>();
        }

        internal uv_buf_t[] GetNativeBuffer()
        {
            Debug.Assert(this.handles.Count == 0);

            IByteBuffer byteBuffer = this.buffer;
            uv_buf_t[] bufs;
            if (byteBuffer.HasArray)
            {
                byte[] array = byteBuffer.Array;
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                this.handles.Add(handle);

                IntPtr arrayHandle = handle.AddrOfPinnedObject();
                bufs = new[] { new uv_buf_t(arrayHandle + this.index, this.length) };
                handle = GCHandle.Alloc(bufs, GCHandleType.Pinned);
                this.handles.Add(handle);
            }
            else
            {
                if (byteBuffer.IoBufferCount == 1)
                {
                    ArraySegment<byte> arraySegment = byteBuffer.GetIoBuffer(this.index, this.length);

                    byte[] array = arraySegment.Array;
                    GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    this.handles.Add(handle);

                    IntPtr arrayHandle = handle.AddrOfPinnedObject();
                    bufs = new[] { new uv_buf_t(arrayHandle + arraySegment.Offset, arraySegment.Count) };
                    handle = GCHandle.Alloc(bufs, GCHandleType.Pinned);
                    this.handles.Add(handle);
                }
                else
                {
                    ArraySegment<byte>[] segments = byteBuffer.GetIoBuffers(this.index, this.length);
                    bufs = new uv_buf_t[segments.Length];
                    for (int i = 0;  i < segments.Length; i++)
                    {
                        byte[] array = segments[i].Array;
                        GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                        this.handles.Add(handle);

                        IntPtr arrayHandle = handle.AddrOfPinnedObject();
                        bufs[i] = new uv_buf_t(arrayHandle + segments[i].Offset, segments[i].Count);
                        handle = GCHandle.Alloc(bufs, GCHandleType.Pinned);
                        this.handles.Add(handle);
                    }
                }
            }

            return bufs;
        }

        void Release()
        {
            GCHandle[] array = this.handles.ToArray();
            this.handles.Clear();

            foreach (GCHandle handle in array)
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        public void Dispose()
        {
            this.Release();
            this.buffer.Release();
        }
    }
}
