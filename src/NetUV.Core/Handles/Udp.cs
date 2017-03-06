// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetUV.Core.Buffers;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    public sealed class Udp : ScheduleHandle
    {
        const int FixedBufferSize = 2048;

        internal static readonly uv_alloc_cb AllocateCallback = OnAllocateCallback;
        internal static readonly uv_udp_recv_cb ReceiveCallback = OnReceiveCallback;

        readonly ByteBufferAllocator allocator;
        readonly BufferQueue bufferQueue;
        readonly WriteRequestPool sendRequestPool;

        Action<Udp, IDatagramReadCompletion> readAction;

        internal Udp(LoopContext loop)
            : this(loop, ByteBufferAllocator.Default)
        { }

        internal Udp(LoopContext loop, ByteBufferAllocator allocator)
            : base(loop, uv_handle_type.UV_UDP)
        {
            Contract.Requires(allocator != null);

            this.allocator = allocator;
            this.bufferQueue = new BufferQueue();
            this.sendRequestPool = new WriteRequestPool(uv_req_type.UV_UDP_SEND);
        }

        public WritableBuffer Allocate(int size)
        {
            Contract.Requires(size > 0 && size < FixedBufferSize);

            this.Validate();
            return new WritableBuffer(this.allocator.Buffer(size));
        }

        public int GetSendBufferSize()
        {
            this.Validate();
            return NativeMethods.SendBufferSize(this.InternalHandle, 0);
        }

        public int SetSendBufferSize(int value)
        {
            Contract.Requires(value > 0);

            this.Validate();
            return NativeMethods.SendBufferSize(this.InternalHandle, value);
        }

        public int GetReceiveBufferSize()
        {
            this.Validate();
            return NativeMethods.ReceiveBufferSize(this.InternalHandle, 0);
        }

        public int SetReceiveBufferSize(int value)
        {
            Contract.Requires(value > 0);

            this.Validate();
            return NativeMethods.ReceiveBufferSize(this.InternalHandle, value);
        }

        public IntPtr GetFileDescriptor()
        {
            this.Validate();
            return NativeMethods.GetFileDescriptor(this.InternalHandle);
        }

        public void RegisterReceiveAction(Action<Udp, IDatagramReadCompletion> action)
        {
            Contract.Requires(action != null);

            if (this.readAction != null)
            {
                throw new InvalidOperationException(
                    $"{nameof(Udp)} data handler has already been registered");
            }

            this.readAction = action;
        }

        public void ReceiveStart()
        {
            this.Validate();
            NativeMethods.UdpReceiveStart(this.InternalHandle);
            Log.TraceFormat("{0} {1} receive started", this.HandleType, this.InternalHandle);
        }

        public void ReceiveStop()
        {
            this.Validate();
            NativeMethods.UdpReceiveStop(this.InternalHandle);
            Log.TraceFormat("{0} {1} receive stopped", this.HandleType, this.InternalHandle);
        }

        public void QueueSend(byte[] array, 
            IPEndPoint remoteEndPoint, 
            Action<Udp, Exception> completion = null)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(remoteEndPoint != null);

            this.QueueSend(array, 0, array.Length, remoteEndPoint, completion);
        }

        public void QueueSend(byte[] array, int offset, int count, 
            IPEndPoint remoteEndPoint, 
            Action<Udp, Exception> completion = null)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);
            Contract.Requires(remoteEndPoint != null);

            ByteBuffer byteBuffer = UnpooledByteBuffer.From(array, offset, count);
            var bufferRef = new BufferRef(byteBuffer, false);
            this.QueueSend(bufferRef, remoteEndPoint, completion);
        }

        public void QueueSend(WritableBuffer writableBuffer, 
            IPEndPoint remoteEndPoint, 
            Action<Udp, Exception> completion = null)
        {
            Contract.Requires(remoteEndPoint != null);
            if (writableBuffer.Index == 0)
            {
                return;
            }

            var bufferRef = new BufferRef(writableBuffer);
            this.QueueSend(bufferRef, remoteEndPoint, completion);
        }

        void QueueSend(BufferRef bufferRef, 
            IPEndPoint remoteEndPoint, 
            Action<Udp, Exception> completion)
        {
            Contract.Requires(bufferRef != null);

            try
            {
                WriteRequest request = this.sendRequestPool.Take();
                Contract.Assert(request != null);

                // ReSharper disable once PossibleNullReferenceException
                request.Prepare(bufferRef,
                    (sendRequest, exception) =>
                    {
                        this.sendRequestPool.Return(sendRequest);
                        completion?.Invoke(this, exception);
                    });

                uv_buf_t[] bufs = request.Bufs;
                NativeMethods.UdpSend(
                    request.InternalHandle, 
                    this.InternalHandle, 
                    remoteEndPoint, 
                    ref bufs);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} faulted.", exception);
                throw;
            }
        }

        public void TrySend(IPEndPoint remoteEndPoint, byte[] array)
        {
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(array != null && array.Length > 0);

            this.TrySend(remoteEndPoint, array, 0, array.Length);
        }

        public unsafe void TrySend(IPEndPoint remoteEndPoint, byte[] array, int offset, int count)
        {
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            this.Validate();
            try
            {
                fixed (byte* memory = array)
                {
                    var buf = new uv_buf_t((IntPtr)memory + offset, count);
                    NativeMethods.UdpTrySend(this.InternalHandle, remoteEndPoint, ref buf);
                }
            }
            catch (Exception exception)
            {
                Log.Debug($"{this.HandleType} Trying to send data to {remoteEndPoint} failed.", exception);
                throw;
            }
        }

        public Udp JoinGroup(IPAddress multicastAddress, IPAddress interfaceAddress = null)
        {
            Contract.Requires(multicastAddress != null);

            this.SetMembership(multicastAddress, interfaceAddress, uv_membership.UV_JOIN_GROUP);
            return this;
        }

        public Udp LeaveGroup(IPAddress multicastAddress, IPAddress interfaceAddress = null)
        {
            Contract.Requires(multicastAddress != null);

            this.SetMembership(multicastAddress, interfaceAddress, uv_membership.UV_LEAVE_GROUP);
            return this;
        }

        void SetMembership(IPAddress multicastAddress, IPAddress interfaceAddress, uv_membership membership)
        {
            this.Validate();

            NativeMethods.UdpSetMembership(this.InternalHandle,
                multicastAddress,
                interfaceAddress,
                membership);
        }

        public Udp Bind(IPEndPoint endPoint, bool reuseAddress = false, bool dualStack = false)
        {
            Contract.Requires(endPoint != null);

            this.Validate();
            NativeMethods.UdpBind(this.InternalHandle, endPoint, reuseAddress, dualStack);

            return this;
        }

        public IPEndPoint GetLocalEndPoint()
        {
            this.Validate();
            return NativeMethods.UdpGetSocketName(this.InternalHandle);
        }

        public Udp MulticastInterface(IPAddress interfaceAddress)
        {
            Contract.Requires(interfaceAddress != null);

            this.Validate();
            NativeMethods.UdpSetMulticastInterface(this.InternalHandle, interfaceAddress);

            return this;
        }

        public Udp MulticastLoopback(bool value)
        {
            this.Validate();
            NativeMethods.UpdSetMulticastLoopback(this.InternalHandle, value);

            return this;
        }

        public Udp MulticastTtl(int value)
        {
            this.Validate();
            NativeMethods.UdpSetMulticastTtl(this.InternalHandle, value);

            return this;
        }

        public Udp Ttl(int value)
        {
            this.Validate();
            NativeMethods.UdpSetTtl(this.InternalHandle, value);

            return this;
        }

        public Udp Broadcast(bool value)
        {
            this.Validate();
            NativeMethods.UdpSetBroadcast(this.InternalHandle, value);

            return this;
        }

        void OnReceivedCallback(ByteBuffer byteBuffer, int status, IPEndPoint remoteEndPoint)
        {
            // status (nread) 
            //     Number of bytes that have been received. 
            //     0 if there is no more data to read. You may discard or repurpose the read buffer. 
            //     Note that 0 may also mean that an empty datagram was received (in this case addr is not NULL). 
            //     < 0 if a transmission error was detected.

            // For status = 0 (Nothing to read)
            if (status >= 0)
            {
                Contract.Assert(byteBuffer != null);
                // ReSharper disable once PossibleNullReferenceException
                Log.DebugFormat("{0} {1} read, buffer length = {2} status = {3}.", this.HandleType, this.InternalHandle, byteBuffer.Count, status);
                this.InvokeRead(byteBuffer, status, remoteEndPoint);
                return;
            }

            Exception exception = NativeMethods.CreateError((uv_err_code)status);
            Log.Error($"{this.HandleType} {this.InternalHandle} read error, status = {status}", exception);

            byteBuffer?.Dispose();
            this.bufferQueue.Clear();
            this.InvokeRead(null, 0, remoteEndPoint, exception);
        }

        void InvokeRead(ByteBuffer byteBuffer, int size, IPEndPoint remoteEndPoint, Exception error = null)
        {
            var completion = new DatagramReadCompletion(
                byteBuffer?.ToReadableBuffer(size) ?? ReadableBuffer.Empty,
                error,
                remoteEndPoint);
            try
            {
                this.readAction?.Invoke(this, completion);
            }
            catch (Exception exception)
            {
                Log.Warn($"{nameof(Udp)} Exception whilst invoking read callback.", exception);
            }
            finally
            {
                completion.Dispose();
            }
        }

        // addr: 
        //     struct sockaddr ontaining the address of the sender. 
        //     Can be NULL. Valid for the duration of the callback only.
        //
        // flags: 
        //     One or more or’ed UV_UDP_* constants. 
        //     Right now only UV_UDP_PARTIAL is used
        static void OnReceiveCallback(IntPtr handle, IntPtr nread, ref uv_buf_t buf, ref sockaddr addr, int flags)
        {
            var udp = HandleContext.GetTarget<Udp>(handle);
            ByteBuffer byteBuffer = udp.GetBuffer();

            int count = (int)nread.ToInt64();
            IPEndPoint remoteEndPoint = count > 0 ? addr.GetIPEndPoint() : null;

            //
            // Indicates message was truncated because read buffer was too small. 
            // The remainder was discarded by the OS. Used in uv_udp_recv_cb.
            // 
            if (flags == (int)uv_udp_flags.UV_UDP_PARTIAL)
            {
                Log.Warn($"{uv_handle_type.UV_UDP} {handle} receive result truncated, buffer size = {byteBuffer.Count}");
            }

            udp.OnReceivedCallback(byteBuffer, count, remoteEndPoint);
        }

        void OnAllocateCallback(out uv_buf_t buf)
        {
            ByteBuffer buffer = this.allocator.Buffer(FixedBufferSize);
            Log.TraceFormat("{0} {1} receive buffer allocated size = {2}", this.HandleType, this.InternalHandle, buffer.Count);

            var bufferRef = new BufferRef(buffer);
            this.bufferQueue.Enqueue(bufferRef);

            // ReSharper disable PossibleNullReferenceException
            uv_buf_t[] bufs = bufferRef.GetBuffer();
            Contract.Assert(bufs != null && bufs.Length > 0);
            buf = bufs[0];
            // ReSharper restore PossibleNullReferenceException
        }

        ByteBuffer GetBuffer()
        {
            ByteBuffer byteBuffer = null;

            BufferRef bufferRef;
            if (this.bufferQueue.TryDequeue(out bufferRef))
            {
                byteBuffer = bufferRef.GetByteBuffer();
            }

            bufferRef?.Dispose();

            return byteBuffer;
        }

        static void OnAllocateCallback(IntPtr handle, IntPtr suggestedSize, out uv_buf_t buf)
        {
            var udp = HandleContext.GetTarget<Udp>(handle);
            udp.OnAllocateCallback(out buf);
        }

        protected override void Close()
        {
            this.readAction = null;
            this.bufferQueue.Dispose();
        }

        public void CloseHandle(Action<Udp> callback = null) =>
            base.CloseHandle(state => callback?.Invoke((Udp)state));

        sealed class DatagramReadCompletion : ReadCompletion, IDatagramReadCompletion
        {
            internal DatagramReadCompletion(ReadableBuffer data, Exception error, IPEndPoint remoteEndPoint)
                : base(data, error)
            {
                this.RemoteEndPoint = remoteEndPoint;
            }

            public IPEndPoint RemoteEndPoint { get; }
        }
    }
}
