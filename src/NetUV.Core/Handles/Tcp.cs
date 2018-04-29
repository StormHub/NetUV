// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetUV.Core.Buffers;
    using NetUV.Core.Native;
    using NetUV.Core.Utilities;

    public sealed class Tcp : ServerStream, IAttributeMap
    {
        internal Tcp(LoopContext loop)
            : base(loop, uv_handle_type.UV_TCP)
        { }

        public int GetSendBufferSize() => this.SendBufferSize(0);

        public int SetSendBufferSize(int value)
        {
            Contract.Requires(value > 0);

            return this.SendBufferSize(value);
        }

        public int GetReceiveBufferSize() => this.ReceiveBufferSize(0);
        

        public int SetReceiveBufferSize(int value)
        {
            Contract.Requires(value > 0);

            return this.ReceiveBufferSize(value);
        }

        public void Shutdown(Action<Tcp, Exception> completedAction = null) => 
            base.Shutdown((state, error) => completedAction?.Invoke((Tcp)state, error));

        public void QueueWrite(byte[] array, Action<Tcp, Exception> completion = null)
        {
            Contract.Requires(array != null);

            this.QueueWrite(array, 0, array.Length, completion);
        }

        public void QueueWrite(byte[] array, int offset, int count, Action<Tcp, Exception> completion = null)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            this.QueueWriteStream(array, offset, count, 
                (state, error) => completion?.Invoke((Tcp)state, error));
        }

        public void QueueWriteStream(WritableBuffer writableBuffer, Action<Tcp, Exception> completion) =>
            base.QueueWriteStream(writableBuffer, (streamHandle, exception) => completion((Tcp)streamHandle, exception));

        public Tcp OnRead(
            Action<Tcp, ReadableBuffer> onAccept,
            Action<Tcp, Exception> onError,
            Action<Tcp> onCompleted = null)
        {
            Contract.Requires(onAccept != null);
            Contract.Requires(onError != null);

            base.OnRead(
                (stream, buffer) => onAccept((Tcp)stream, buffer),
                (stream, error) => onError((Tcp)stream, error),
                stream => onCompleted?.Invoke((Tcp)stream));

            return this;
        }

        public Tcp OnRead(Action<Tcp, IStreamReadCompletion> onRead)
        {
            Contract.Requires(onRead != null);

            base.OnRead((stream, completion) => onRead((Tcp)stream, completion));
            return this;
        }

        public Tcp Bind(IPEndPoint endPoint, bool dualStack = false)
        {
            Contract.Requires(endPoint != null);

            this.Validate();
            NativeMethods.TcpBind(this.InternalHandle, endPoint, dualStack);

            return this;
        }

        public IPEndPoint GetLocalEndPoint()
        {
            this.Validate();
            return NativeMethods.TcpGetSocketName(this.InternalHandle);
        }

        public IPEndPoint GetPeerEndPoint()
        {
            this.Validate();
            return NativeMethods.TcpGetPeerName(this.InternalHandle);
        }

        public Tcp NoDelay(bool value)
        {
            this.Validate();
            NativeMethods.TcpSetNoDelay(this.InternalHandle, value);

            return this;
        }

        public Tcp KeepAlive(bool value, int delay)
        {
            this.Validate();
            NativeMethods.TcpSetKeepAlive(this.InternalHandle, value, delay);

            return this;
        }

        public Tcp SimultaneousAccepts(bool value)
        {
            this.Validate();
            NativeMethods.TcpSimultaneousAccepts(this.InternalHandle, value);

            return this;
        }

        protected internal override unsafe StreamHandle NewStream()
        {
            IntPtr loopHandle = ((uv_stream_t*)this.InternalHandle)->loop;
            var loop = HandleContext.GetTarget<LoopContext>(loopHandle);

            var client = new Tcp(loop);
            NativeMethods.StreamAccept(this.InternalHandle, client.InternalHandle);
            client.ReadStart();

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1} client {2} accepted", 
                    this.HandleType, this.InternalHandle, client.InternalHandle);
            }

            return client;
        }

        public Tcp Listen(Action<Tcp, Exception> onConnection, int backlog = DefaultBacklog)
        {
            Contract.Requires(onConnection != null);
            Contract.Requires(backlog > 0);

            this.StreamListen((handle, exception) => onConnection((Tcp)handle, exception), backlog);
            return this;
        }

        public void CloseHandle(Action<Tcp> onClosed = null)
        {
            Action<ScheduleHandle> handler = null;
            if (onClosed != null)
            {
                handler = state => onClosed((Tcp)state);
            }

            base.CloseHandle(handler);
        }

        public IAttribute<T> GetAttribute<T>(AttributeKey<T> key) where T : class
        {
            return ((IAttributeMap)attributeMap).GetAttribute(key);
        }

        public bool HasAttribute<T>(AttributeKey<T> key) where T : class
        {
            return ((IAttributeMap)attributeMap).HasAttribute(key);
        }

        DefaultAttributeMap attributeMap = new DefaultAttributeMap();
    }
}
