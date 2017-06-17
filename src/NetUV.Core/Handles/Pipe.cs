// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Native;

    public sealed class Pipe : ServerStream
    {
        bool ipc;

        internal Pipe(LoopContext loop, bool ipc = false)
            : base(loop, uv_handle_type.UV_NAMED_PIPE, ipc)
        {
            this.ipc = ipc;
        }

        public int GetSendBufferSize()
        {
            if (Platform.IsWindows)
            {
                throw new PlatformNotSupportedException(
                    $"{this.HandleType} send buffer size setting not supported on Windows");
            }

            return this.SendBufferSize(0);
        }

        public int SetSendBufferSize(int value)
        {
            Contract.Requires(value > 0);

            if (Platform.IsWindows)
            {
                throw new PlatformNotSupportedException(
                    $"{this.HandleType} send buffer size setting not supported on Windows");
            }

            return this.SendBufferSize(value);
        }

        public int GetReceiveBufferSize()
        {
            if (Platform.IsWindows)
            {
                throw new PlatformNotSupportedException(
                    $"{this.HandleType} send buffer size setting not supported on Windows");
            }

            return this.ReceiveBufferSize(0);
        }

        public int SetReceiveBufferSize(int value)
        {
            Contract.Requires(value > 0);

            if (Platform.IsWindows)
            {
                throw new PlatformNotSupportedException(
                    $"{this.HandleType} send buffer size setting not supported on Windows");
            }

            return this.ReceiveBufferSize(value);
        }

        public Pipe OnRead(
            Action<Pipe, ReadableBuffer> onAccept,
            Action<Pipe, Exception> onError,
            Action<Pipe> onCompleted = null)
        {
            Contract.Requires(onAccept != null);
            Contract.Requires(onError != null);

            base.OnRead(
                (stream, buffer) => onAccept((Pipe)stream, buffer), 
                (stream, error) => onError((Pipe)stream, error), 
                stream => onCompleted?.Invoke((Pipe)stream));

            return this;
        }

        public Pipe OnRead(Action<Pipe, IStreamReadCompletion> onRead)
        {
            Contract.Requires(onRead != null);

            base.OnRead((stream, completion) => onRead((Pipe)stream, completion));
            return this;
        }

        public Pipe Bind(string name)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));

            this.Validate();
            NativeMethods.PipeBind(this.InternalHandle, name);

            return this;
        }

        public string GetSocketName()
        {
            this.Validate();
            return NativeMethods.PipeGetSocketName(this.InternalHandle);
        }

        public string GetPeerName()
        {
            this.Validate();
            return NativeMethods.PipeGetPeerName(this.InternalHandle);
        }

        public void PendingInstances(int count)
        {
            Contract.Requires(count > 0);

            this.Validate();
            NativeMethods.PipePendingInstances(this.InternalHandle, count);
        }

        public int PendingCount()
        {
            this.Validate();
            return NativeMethods.PipePendingCount(this.InternalHandle);
        }

        public unsafe StreamHandle CreatePendingType()
        {
            this.Validate();

            StreamHandle handle = null;

            int count = this.PendingCount();
            if (count > 0)
            {
                IntPtr loopHandle = ((uv_stream_t*)this.InternalHandle)->loop;
                var loop = HandleContext.GetTarget<LoopContext>(loopHandle);
                uv_handle_type handleType = NativeMethods.PipePendingType(this.InternalHandle);

                if (handleType == uv_handle_type.UV_TCP)
                {
                    handle = new Tcp(loop);
                }
                else if (handleType == uv_handle_type.UV_NAMED_PIPE)
                {
                    handle = new Pipe(loop);
                }
                else
                {
                    throw new InvalidOperationException($"{handleType} not supported or IPC over Pipe is disabled.");
                }

                NativeMethods.StreamAccept(this.InternalHandle, handle.InternalHandle);
            }

            return handle;
        }

        protected internal override unsafe StreamHandle NewStream()
        {
            IntPtr loopHandle = ((uv_stream_t*)this.InternalHandle)->loop;
            var loop = HandleContext.GetTarget<LoopContext>(loopHandle);
            uv_handle_type type = ((uv_stream_t*)this.InternalHandle)->type;

            StreamHandle client;
            if (type == uv_handle_type.UV_NAMED_PIPE)
            {
                client = new Pipe(loop, this.ipc);
            }
            else if (type == uv_handle_type.UV_TCP)
            {
                client = new Tcp(loop);
            }
            else
            {
                throw new InvalidOperationException($"Pipe IPC handle {type} not supported");
            }

            NativeMethods.StreamAccept(this.InternalHandle, client.InternalHandle);
            if (!this.ipc)
            {
                client.ReadStart();
            }

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1} client {2} accepted. (IPC : {3})", this.HandleType, this.InternalHandle, client.InternalHandle, this.ipc);
            }

            return client;
        }

        public Pipe Listen(Action<Pipe, Exception> onConnection, int backlog = DefaultBacklog, bool useIpc = false)
        {
            Contract.Requires(onConnection != null);
            Contract.Requires(backlog > 0);

            this.ipc = useIpc;
            this.StreamListen((handle, exception) => onConnection((Pipe)handle, exception), backlog);

            return this;
        }

        public void Shutdown(Action<Pipe, Exception> completedAction = null) =>
            base.Shutdown((state, error) => completedAction?.Invoke((Pipe)state, error));

        public void CloseHandle(Action<Pipe> onClosed = null)
        {
            Action<ScheduleHandle> handler = null;
            if (onClosed != null)
            {
                handler = state => onClosed((Pipe)state);
            }

            base.CloseHandle(handler);
        }
    }
}
