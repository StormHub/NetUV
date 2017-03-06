// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    public abstract class StreamHandle : ScheduleHandle
    {
        internal static readonly uv_alloc_cb AllocateCallback = OnAllocateCallback;
        internal static readonly uv_read_cb ReadCallback = OnReadCallback;

        readonly Pipeline pipeline;

        internal StreamHandle(
            LoopContext loop,
            uv_handle_type handleType, 
            params object[] args)
            : base(loop, handleType, args)
        {
            this.pipeline = new Pipeline(this);
        }

        public bool IsReadable =>
            NativeMethods.IsStreamReadable(this.InternalHandle);

        public bool IsWritable =>
            NativeMethods.IsStreamWritable(this.InternalHandle);

        protected int SendBufferSize(int value)
        {
            Contract.Requires(value >= 0);

            this.Validate();
            return NativeMethods.SendBufferSize(this.InternalHandle, value);
        }

        protected int ReceiveBufferSize(int value)
        {
            Contract.Requires(value >= 0);

            this.Validate();
            return NativeMethods.ReceiveBufferSize(this.InternalHandle, value);
        }

        public IntPtr GetFileDescriptor()
        {
            this.Validate();
            return NativeMethods.GetFileDescriptor(this.InternalHandle);
        }

        public unsafe long GetWriteQueueSize()
        {
            this.Validate();
            return (((uv_stream_t *)this.InternalHandle)->write_queue_size).ToInt64();
        }

        internal WritableBuffer Allocate(int size)
        {
            Contract.Requires(size > 0);

            return this.pipeline.Allocator.Buffer(size);
        }

        internal void RegisterReadAction(Action<StreamHandle, IStreamReadCompletion> readAction)
        {
            Contract.Requires(readAction != null);

            this.pipeline.ReadAction = readAction;
        }

        protected internal void ShutdownStream(Action<StreamHandle, Exception> completion = null)
        {
            if (!this.IsValid)
            {
                return;
            }

            StreamShutdown streamShutdown = null;
            try
            {
                streamShutdown = new StreamShutdown(this, completion);
            }
            catch (Exception exception)
            {
                Exception error = exception;

                ErrorCode? errorCode = (error as OperationException)?.ErrorCode;
                if (errorCode == ErrorCode.EPIPE)
                {
                    // It is ok if the stream is already down
                    error = null;
                }
                if (error != null)
                {
                    Log.Error($"{this.HandleType} {this.InternalHandle} failed to shutdown.", error);
                }

                StreamShutdown.Completed(completion, this, error);
                streamShutdown?.Dispose();
            }
        }

        public void CloseHandle(Action<StreamHandle> callback = null) =>
            base.CloseHandle(state => callback?.Invoke((StreamHandle)state));

        protected internal void QueueWriteStream(WritableBuffer writableBuffer, 
            Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(completion != null);

            if (writableBuffer.Index == 0)
            {
                return;
            }

            var bufferRef = new BufferRef(writableBuffer);
            this.pipeline.QueueWrite(bufferRef, completion);
        }

        protected internal void QueueWriteStream(byte[] array, int offset, int count, 
            Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            ByteBuffer byteBuffer = UnpooledByteBuffer.From(array, offset, count);
            var bufferRef = new BufferRef(byteBuffer, false);
            this.pipeline.QueueWrite(bufferRef, completion);
        }

        internal void WriteStream(WriteRequest request)
        {
            Contract.Requires(request != null);

            this.Validate();
            try
            {
                uv_buf_t[] bufs = request.Bufs;

                NativeMethods.WriteStream(
                    request.InternalHandle, 
                    this.InternalHandle,
                    ref bufs);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} Failed to write data {request}.", exception);
                throw;
            }
        }

        public void TryWrite(byte[] array)
        {
            Contract.Requires(array != null && array.Length > 0);

            this.TryWrite(array, 0, array.Length);
        }

        internal unsafe void TryWrite(byte[] array, int offset, int count)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            this.Validate();
            try
            {
                fixed (byte* memory = array)
                {
                    var buf = new uv_buf_t((IntPtr)memory + offset, count);
                    NativeMethods.TryWriteStream(this.InternalHandle, ref buf);
                }
            }
            catch (Exception exception)
            {
                Log.Debug($"{this.HandleType} Trying to write data failed.", exception);
                throw;
            }
        }

        internal void ReadStart()
        {
            this.Validate();
            NativeMethods.StreamReadStart(this.InternalHandle);
            Log.TraceFormat("{0} {1} Read started.", this.HandleType, this.InternalHandle);
        }

        internal void ReadStop()
        {
            if (!this.IsValid)
            {
                return;
            }

            // This function is idempotent and may be safely called on a stopped stream.
            NativeMethods.StreamReadStop(this.InternalHandle);
            Log.TraceFormat("{0} {1} Read stopped.", this.HandleType, this.InternalHandle);
        }

        protected override void Close() => this.pipeline.Dispose();

        public IStream CreateStream() => this.pipeline.CreateStream();

        protected IStream<T> CreateStream<T>() 
            where T : StreamHandle => this.pipeline.CreateStream<T>();

        void OnReadCallback(ByteBuffer byteBuffer, int status)
        {
            //
            //  nread is > 0 if there is data available or < 0 on error.
            //  When we’ve reached EOF, nread will be set to UV_EOF.
            //  When nread < 0, the buf parameter might not point to a valid buffer; 
            //  in that case buf.len and buf.base are both set to 0
            //

            // For status = 0 (Nothing to read)
            if (status >= 0) 
            {
                Contract.Assert(byteBuffer != null);
                // ReSharper disable once PossibleNullReferenceException
                Log.DebugFormat("{0} {1} read, buffer length = {2} status = {3}.", this.HandleType, this.InternalHandle, byteBuffer.Count, status);
                this.pipeline.OnReadCompleted(byteBuffer, status);
                return;
            }

            Exception exception = null;
            if (status != (int)uv_err_code.UV_EOF) // Stream end is not an error
            {
                exception = NativeMethods.CreateError((uv_err_code)status);
                Log.Error($"{this.HandleType} {this.InternalHandle} read error, status = {status}", exception);
            }

            Log.DebugFormat("{0} {1} read completed.", this.HandleType, this.InternalHandle);
            byteBuffer?.Dispose();

            this.pipeline.OnReadCompleted(exception);
            this.ReadStop();
        }

        static void OnReadCallback(IntPtr handle, IntPtr nread, ref uv_buf_t buf)
        {
            var stream = HandleContext.GetTarget<StreamHandle>(handle);
            ByteBuffer byteBuffer = stream.pipeline.GetBuffer(ref buf);
            stream.OnReadCallback(byteBuffer, (int)nread.ToInt64());
        }

        void OnAllocateCallback(out uv_buf_t buf)
        {
            BufferRef bufferRef = this.pipeline.AllocateReadBuffer();
            Contract.Assert(bufferRef != null);
            // ReSharper disable PossibleNullReferenceException
            uv_buf_t[] bufs = bufferRef.GetBuffer();
            Contract.Assert(bufs != null && bufs.Length > 0);
            buf = bufs[0];
            // ReSharper restore PossibleNullReferenceException
        }

        static void OnAllocateCallback(IntPtr handle, IntPtr suggestedSize, out uv_buf_t buf)
        {
            var stream = HandleContext.GetTarget<StreamHandle>(handle);
            stream.OnAllocateCallback(out buf);
        }
    }
}
