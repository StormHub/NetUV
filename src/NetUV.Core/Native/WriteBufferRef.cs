
namespace NetUV.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using NetUV.Core.Buffers;

    sealed class WriteBufferRef : IDisposable
    {
        const int DefaultSize = 32;

        readonly IByteBuffer buffer;
        readonly List<GCHandle> handles;

        uv_buf_t[] bufs;
        int size;
        GCHandle pin;

        internal WriteBufferRef(IByteBuffer buffer)
        {
            Debug.Assert(buffer != null);

            this.buffer = buffer;
            this.handles = new List<GCHandle>();
            this.size = 1;
            this.bufs = new uv_buf_t[DefaultSize];
            this.pin = GCHandle.Alloc(this.bufs, GCHandleType.Pinned);
        }

        internal void Prepare()
        {
            IByteBuffer byteBuffer = this.buffer;
            int index = byteBuffer.ReaderIndex;
            int length = byteBuffer.ReadableBytes;
            IntPtr addr = byteBuffer.AddressOfPinnedMemory();
            if (addr != IntPtr.Zero)
            {
                this.bufs[0] = new uv_buf_t(addr + index, length);
                this.size = 1;
                return;
            }

            if (byteBuffer.HasArray)
            {
                byte[] array = byteBuffer.Array;
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                this.handles.Add(handle);

                addr = handle.AddrOfPinnedObject();
                this.bufs[0] = new uv_buf_t(addr + this.buffer.ArrayOffset + index, length);
                this.size = 1;
                return;
            }

            if (byteBuffer.IoBufferCount == 1)
            {
                ArraySegment<byte> arraySegment = byteBuffer.GetIoBuffer(index, length);

                byte[] array = arraySegment.Array;
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                this.handles.Add(handle);

                addr = handle.AddrOfPinnedObject();
                this.bufs[0] = new uv_buf_t(addr + arraySegment.Offset, arraySegment.Count);
                this.size = 1;
                return;
            }

            ArraySegment<byte>[] segments = byteBuffer.GetIoBuffers(index, length);
            if (segments.Length > this.bufs.Length)
            {
                if (this.pin.IsAllocated)
                {
                    this.pin.Free();
                }
                this.bufs = new uv_buf_t[segments.Length];
                this.pin = GCHandle.Alloc(this.bufs, GCHandleType.Pinned);
            }

            for (int i = 0; i < segments.Length; i++)
            {
                GCHandle handle = GCHandle.Alloc(segments[i].Array, GCHandleType.Pinned);
                this.handles.Add(handle);

                addr = handle.AddrOfPinnedObject();
                this.bufs[i] = new uv_buf_t(addr + segments[i].Offset, segments[i].Count);
            }

            this.size = segments.Length;
        }

        public ref uv_buf_t[] Bufs => ref this.bufs;

        public ref int Size => ref this.size;

        void Release()
        {
            if (this.handles.Count > 0)
            {
                for (int i = 0; i < this.handles.Count; i++)
                {
                    if (this.handles[i].IsAllocated)
                    {
                        this.handles[i].Free();
                    }
                }
                this.handles.Clear();
            }
        }

        public void Dispose()
        {
            this.Release();
            if (this.pin.IsAllocated)
            {
                this.pin.Free();
            }
        }
    }
}
