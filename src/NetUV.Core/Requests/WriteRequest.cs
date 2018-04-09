// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace NetUV.Core.Requests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using NetUV.Core.Buffers;
    using NetUV.Core.Common;
    using NetUV.Core.Native;

    sealed class WriteRequest : ScheduleRequest
    {
        const int MaximumLimit = 32;

        internal static readonly uv_watcher_cb WriteCallback = OnWriteCallback;
        static readonly int BufferSize;

        readonly RequestContext requestContext;
        readonly ThreadLocalPool.Handle recyclerHandle;
        readonly List<GCHandle> handles;

        IntPtr bufs;
        GCHandle pin;
        int count;

        uv_buf_t[] bufsArray;

        Action<WriteRequest, Exception> completion;

        static WriteRequest()
        {
            BufferSize = Marshal.SizeOf<uv_buf_t>();
        }

        internal WriteRequest(uv_req_type requestType, ThreadLocalPool.Handle recyclerHandle)
            : base(requestType)
        {
            Debug.Assert(requestType == uv_req_type.UV_WRITE || requestType == uv_req_type.UV_UDP_SEND);

            this.requestContext = new RequestContext(requestType, BufferSize * MaximumLimit, this);
            this.recyclerHandle = recyclerHandle;
            this.handles = new List<GCHandle>();

            IntPtr addr = this.requestContext.Handle;
            this.bufs = addr + this.requestContext.HandleSize;
            this.pin = GCHandle.Alloc(addr, GCHandleType.Pinned);
            this.count = 0;
        }

        internal override IntPtr InternalHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.requestContext.Handle;
        }

        internal void Prepare(IByteBuffer buf, Action<WriteRequest, Exception> callback)
        {
            Debug.Assert(buf != null && callback != null);

            if (!this.requestContext.IsValid)
            {
                ThrowHelper.ThrowInvalidOperationException_WriteRequest();
            }

            this.completion = callback;
            int len = buf.ReadableBytes;

            IntPtr addr = IntPtr.Zero;
            if (buf.HasMemoryAddress)
            {
                addr = buf.AddressOfPinnedMemory();
            }

            if (addr != IntPtr.Zero)
            {
                this.Add(addr, buf.ReaderIndex, len);
                return;
            }

            int bufferCount = buf.IoBufferCount;
            if (bufferCount == 1)
            {
                ArraySegment<byte> arraySegment = buf.GetIoBuffer();

                byte[] array = arraySegment.Array;
                GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                this.handles.Add(handle);

                addr = handle.AddrOfPinnedObject();
                this.Add(addr, arraySegment.Offset, arraySegment.Count);
                return;
            }

            ArraySegment<byte>[] segments = buf.GetIoBuffers();
            if (segments.Length <= MaximumLimit)
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    GCHandle handle = GCHandle.Alloc(segments[i].Array, GCHandleType.Pinned);
                    this.handles.Add(handle);

                    addr = handle.AddrOfPinnedObject();
                    this.Add(addr, segments[i].Offset, segments[i].Count);
                }
                return;
            }

            this.bufsArray = new uv_buf_t[segments.Length];
            GCHandle bufsPin = GCHandle.Alloc(this.bufsArray, GCHandleType.Pinned);
            this.handles.Add(bufsPin);

            for (int i = 0; i < segments.Length; i++)
            {
                GCHandle handle = GCHandle.Alloc(segments[i].Array, GCHandleType.Pinned);
                this.handles.Add(handle);

                addr = handle.AddrOfPinnedObject();
                this.bufsArray[i] = new uv_buf_t(addr + segments[i].Offset, segments[i].Count);
            }
            this.count = segments.Length;
        }

        void Add(IntPtr addr, int offset, int len)
        {
            IntPtr baseOffset = this.bufs + BufferSize * this.count;
            ++this.count;
            uv_buf_t.InitMemory(baseOffset, addr + offset, len);
        }

        internal unsafe uv_buf_t* Bufs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.bufsArray == null ? (uv_buf_t*)this.bufs : (uv_buf_t*)Unsafe.AsPointer(ref this.bufsArray[0]);
        }

        internal ref int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this.count;
        }

        internal void Release()
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

            this.bufsArray = null;
            this.completion = null;
            this.count = 0;
            this.recyclerHandle.Release(this);
        }

        void Free()
        {
            this.Release();
            if (this.pin.IsAllocated)
            {
                this.pin.Free();
            }
            this.bufs = IntPtr.Zero;
        }

        void OnWriteCallback(int status)
        {
            OperationException error = null;
            if (status < 0)
            {
                error = NativeMethods.CreateError((uv_err_code)status);
            }

            Action<WriteRequest, Exception> callback = this.completion;
            this.Release();
            callback?.Invoke(this, error);
        }

        static void OnWriteCallback(IntPtr handle, int status)
        {
            var request = RequestContext.GetTarget<WriteRequest>(handle);
            request.OnWriteCallback(status);
        }

        protected override void Close()
        {
            if (this.bufs != IntPtr.Zero)
            {
                this.Free();
            }
            this.requestContext.Dispose();
        }
    }
}
