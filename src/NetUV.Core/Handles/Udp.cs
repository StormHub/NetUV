// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetUV.Core.Buffers;
    using NetUV.Core.Common;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    public sealed class Udp : ScheduleHandle
    {
        const int DefaultPoolSize = 1024;
        const int FixedBufferSize = 2048;

        internal static readonly uv_alloc_cb AllocateCallback = OnAllocateCallback;
        internal static readonly uv_udp_recv_cb ReceiveCallback = OnReceiveCallback;

        static readonly ThreadLocalPool<SendRequest> Recycler =
            new ThreadLocalPool<SendRequest>(handle => new SendRequest(handle), DefaultPoolSize);

        readonly PooledByteBufferAllocator allocator;
        readonly HeapBufferQueue bufferQueue;

        Action<Udp, IDatagramReadCompletion> readAction;

        internal Udp(LoopContext loop)
            : this(loop, PooledByteBufferAllocator.Default)
        { }

        internal Udp(LoopContext loop, PooledByteBufferAllocator allocator)
            : base(loop, uv_handle_type.UV_UDP)
        {
            Contract.Requires(allocator != null);

            this.allocator = allocator;
            this.bufferQueue = new HeapBufferQueue();
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

        public WritableBuffer Allocate()
        {
            IByteBuffer buffer = this.allocator.HeapBuffer();
            return new WritableBuffer(buffer);
        }

        public void OnReceive(Action<Udp, IDatagramReadCompletion> action)
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
            if (Log.IsTraceEnabled)
            {
                Log.TraceFormat("{0} {1} receive started", this.HandleType, this.InternalHandle);
            }
        }

        public void ReceiveStop()
        {
            this.Validate();
            NativeMethods.UdpReceiveStop(this.InternalHandle);
            if (Log.IsTraceEnabled)
            {
                Log.TraceFormat("{0} {1} receive stopped", this.HandleType, this.InternalHandle);
            }
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
            Contract.Requires(array != null);
            Contract.Requires(remoteEndPoint != null);

            IByteBuffer buffer = Unpooled.WrappedBuffer(array, offset, count);
            var bufferRef = new ByteBufferRef(buffer, buffer.ReaderIndex, count);
            this.QueueSend(bufferRef, remoteEndPoint, completion);
        }

        public void QueueSend(WritableBuffer writableBuffer, 
            IPEndPoint remoteEndPoint, 
            Action<Udp, Exception> completion = null)
        {
            Contract.Requires(remoteEndPoint != null);

            IByteBuffer buffer = writableBuffer.GetBuffer();
            if (buffer == null || !buffer.IsReadable())
            {
                return;
            }

            var bufferRef = new ByteBufferRef(buffer, buffer.ReaderIndex, buffer.ReadableBytes);
            this.QueueSend(bufferRef, remoteEndPoint, completion);
        }

        void QueueSend(ByteBufferRef bufferRef, 
            IPEndPoint remoteEndPoint, 
            Action<Udp, Exception> completion)
        {
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(bufferRef != null);

            try
            {
                SendRequest request = Recycler.Take();
                Debug.Assert(request != null);
                request.Prepare(bufferRef,
                    (sendRequest, exception) => completion?.Invoke(this, exception));

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

        void OnReceivedCallback(IByteBuffer byteBuffer, int status, IPEndPoint remoteEndPoint)
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

                if (Log.IsDebugEnabled)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    Log.DebugFormat("{0} {1} read, buffer length = {2} status = {3}.",
                        this.HandleType, this.InternalHandle, byteBuffer.Capacity, status);
                }

                this.InvokeRead(byteBuffer, status, remoteEndPoint);
                return;
            }

            Exception exception = NativeMethods.CreateError((uv_err_code)status);
            Log.Error($"{this.HandleType} {this.InternalHandle} read error, status = {status}", exception);

            this.bufferQueue.Clear();
            this.InvokeRead(byteBuffer, 0, remoteEndPoint, exception);
        }

        void InvokeRead(IByteBuffer byteBuffer, int size, IPEndPoint remoteEndPoint, Exception error = null)
        {
            if (size == 0 && error == null)
            {
                // Filter out empty data received if not an error
                //
                // On windows the udp receive actually been call with empty data 
                // for broadcast, on Linux, the receive is not called at all.
                //
                byteBuffer?.Release();
                return;
            }

            ReadableBuffer buffer = size > 0 ? new ReadableBuffer(byteBuffer, size) : ReadableBuffer.Empty;
            var completion = new DatagramReadCompletion(ref buffer, error, remoteEndPoint);
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
            IByteBuffer byteBuffer = udp.GetBuffer();

            int count = (int)nread.ToInt64();
            IPEndPoint remoteEndPoint = count > 0 ? addr.GetIPEndPoint() : null;

            //
            // Indicates message was truncated because read buffer was too small. 
            // The remainder was discarded by the OS. Used in uv_udp_recv_cb.
            // 
            if (flags == (int)uv_udp_flags.UV_UDP_PARTIAL)
            {
                Log.Warn($"{uv_handle_type.UV_UDP} {handle} receive result truncated, buffer size = {byteBuffer.Capacity}");
            }

            udp.OnReceivedCallback(byteBuffer, count, remoteEndPoint);
        }

        void OnAllocateCallback(out uv_buf_t buf)
        {
            var buffer = (PooledHeapByteBuffer)this.allocator.Buffer(FixedBufferSize);
            if (Log.IsTraceEnabled)
            {
                Log.TraceFormat("{0} {1} receive buffer allocated size = {2}", this.HandleType, this.InternalHandle, buffer.Capacity);
            }

            var bufferRef = new HeapBufferRef(buffer);
            this.bufferQueue.Enqueue(bufferRef);

            uv_buf_t[] bufs = bufferRef.GetNativeBuffer();
            Debug.Assert(bufs != null && bufs.Length > 0);

            buf = bufs[0];
        }

        IByteBuffer GetBuffer()
        {
            IByteBuffer byteBuffer = null;
            HeapBufferRef bufferRef = null;

            try
            {
                if (this.bufferQueue.TryDequeue(out bufferRef))
                {
                    byteBuffer = bufferRef.GetHeapBuffer();
                }
            }
            finally
            {
                bufferRef?.Dispose();
            }

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

        public void CloseHandle(Action<Udp> onClosed = null)
        {
            Action<ScheduleHandle> handler = null;
            if (onClosed != null)
            {
                handler = state => onClosed((Udp)state);
            }

            base.CloseHandle(handler);
        }

        sealed class DatagramReadCompletion : ReadCompletion, IDatagramReadCompletion
        {
            internal DatagramReadCompletion(ref ReadableBuffer data, Exception error, IPEndPoint remoteEndPoint)
                : base(ref data, error)
            {
                this.RemoteEndPoint = remoteEndPoint;
            }

            public IPEndPoint RemoteEndPoint { get; }
        }
    }
}
