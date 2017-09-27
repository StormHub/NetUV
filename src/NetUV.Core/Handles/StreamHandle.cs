// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics;
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

        public WritableBuffer Allocate() => this.pipeline.Allocate();

        public void OnRead(
            Action<StreamHandle, ReadableBuffer> onAccept,
            Action<StreamHandle, Exception> onError,
            Action<StreamHandle> onCompleted = null)
        {
            Contract.Requires(onAccept != null);
            Contract.Requires(onError != null);

            var consumer = new StreamConsumer<StreamHandle>(onAccept, onError, onCompleted);
            this.pipeline.Consumer(consumer);
        }

        public void OnRead(Action<StreamHandle, IStreamReadCompletion> onRead)
        {
            Contract.Requires(onRead != null);

            var consumer = new ReadStreamConsumer<StreamHandle>(onRead);
            this.pipeline.Consumer(consumer);
        }

        public void Shutdown(Action<StreamHandle, Exception> completion = null)
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

        public void CloseHandle(Action<StreamHandle> callback = null)
        {
            Action<ScheduleHandle> handler = null;
            if (callback != null)
            {
                handler = state => callback((StreamHandle)state);
            }

            base.CloseHandle(handler);
        }

        public void QueueWriteStream(WritableBuffer writableBuffer, 
            Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(completion != null);

            IByteBuffer buffer = writableBuffer.GetBuffer();
            if (buffer == null  || !buffer.IsReadable())
            {
                return;
            }

            var bufferRef = new ByteBufferRef(buffer, buffer.ReaderIndex, buffer.ReadableBytes);
            this.pipeline.QueueWrite(bufferRef, completion);
        }

        public void QueueWriteStream(WritableBuffer writableBuffer, StreamHandle sendHandle,
            Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(completion != null);
            Contract.Requires(sendHandle != null);

            IByteBuffer buffer = writableBuffer.GetBuffer();
            if (buffer == null || !buffer.IsReadable())
            {
                return;
            }

            var bufferRef = new ByteBufferRef(buffer, buffer.ReaderIndex, buffer.ReadableBytes);
            this.pipeline.QueueWrite(bufferRef, sendHandle, completion);
        }

        public void QueueWriteStream(byte[] array, Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(array != null && array.Length > 0);

            this.QueueWriteStream(array, 0, array.Length, completion);
        }

        public void QueueWriteStream(byte[] array, int offset, int count, 
            Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            IByteBuffer buffer = Unpooled.WrappedBuffer(array, offset, count);
            var bufferRef = new ByteBufferRef(buffer, buffer.ReaderIndex, count);
            this.pipeline.QueueWrite(bufferRef, completion);
        }

        public void QueueWriteStream(byte[] array, StreamHandle sendHandle, 
            Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(array != null && array.Length > 0);

            this.QueueWriteStream(array, 0, array.Length, sendHandle, completion);
        }

        public void QueueWriteStream(byte[] array, int offset, int count, 
            StreamHandle sendHandle,
            Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(array != null && array.Length > 0);
            Contract.Requires(offset >= 0 && count > 0);
            Contract.Requires((offset + count) <= array.Length);

            IByteBuffer buffer = Unpooled.WrappedBuffer(array, offset, count);
            var bufferRef = new ByteBufferRef(buffer, buffer.ReaderIndex, count);
            this.pipeline.QueueWrite(bufferRef, sendHandle, completion);
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

        internal void WriteStream(WriteRequest request, StreamHandle sendHandle)
        {
            Contract.Requires(request != null);
            Contract.Requires(sendHandle != null);

            this.Validate();
            try
            {
                uv_buf_t[] bufs = request.Bufs;

                NativeMethods.WriteStream(
                    request.InternalHandle,
                    this.InternalHandle,
                    ref bufs, 
                    sendHandle.InternalHandle);
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
            if (Log.IsTraceEnabled)
            {
                Log.TraceFormat("{0} {1} Read started.", this.HandleType, this.InternalHandle);
            }
        }

        internal void ReadStop()
        {
            if (!this.IsValid)
            {
                return;
            }

            // This function is idempotent and may be safely called on a stopped stream.
            NativeMethods.StreamReadStop(this.InternalHandle);
            if (Log.IsTraceEnabled)
            {
                Log.TraceFormat("{0} {1} Read stopped.", this.HandleType, this.InternalHandle);
            }
        }

        protected override void Close() => this.pipeline.Dispose();

        void OnReadCallback(IByteBuffer byteBuffer, int status)
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

                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat("{0} {1} read, buffer length = {2} status = {3}.", this.HandleType, this.InternalHandle, byteBuffer?.Capacity, status);
                }

                this.pipeline.OnReadCompleted(byteBuffer, status);
                return;
            }

            Exception exception = null;
            if (status != (int)uv_err_code.UV_EOF) // Stream end is not an error
            {
                exception = NativeMethods.CreateError((uv_err_code)status);
                Log.Error($"{this.HandleType} {this.InternalHandle} read error, status = {status}", exception);
            }
            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1} read completed.", this.HandleType, this.InternalHandle);
            }

            this.pipeline.OnReadCompleted(byteBuffer, exception);
            this.ReadStop();
        }

        static void OnReadCallback(IntPtr handle, IntPtr nread, ref uv_buf_t buf)
        {
            var stream = HandleContext.GetTarget<StreamHandle>(handle);
            IByteBuffer byteBuffer = stream.pipeline.GetBuffer(ref buf);
            stream.OnReadCallback(byteBuffer, (int)nread.ToInt64());
        }

        void OnAllocateCallback(out uv_buf_t buf)
        {
            HeapBufferRef bufferRef = this.pipeline.AllocateReadBuffer();
            uv_buf_t[] bufs = bufferRef.GetNativeBuffer();

            Debug.Assert(bufs != null);
            buf = bufs[0];
        }

        static void OnAllocateCallback(IntPtr handle, IntPtr suggestedSize, out uv_buf_t buf)
        {
            var stream = HandleContext.GetTarget<StreamHandle>(handle);
            stream.OnAllocateCallback(out buf);
        }
    }
}
