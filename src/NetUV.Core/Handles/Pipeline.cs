// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Logging;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    sealed class Pipeline : IDisposable
    {
        static readonly ILog Log = LogFactory.ForContext<Pipeline>();

        readonly StreamHandle streamHandle;
        readonly WriteRequestPool writeRequestPool;
        readonly ByteBufferAllocator allocator;
        readonly ReceiveBufferSizeEstimate receiveBufferSizeEstimate;
        readonly BufferQueue bufferQueue;

        Action<StreamHandle, IStreamReadCompletion> readAction;

        internal Pipeline(StreamHandle streamHandle) 
            : this(streamHandle, ByteBufferAllocator.Default)
        { }

        internal Pipeline(StreamHandle streamHandle, ByteBufferAllocator allocator)
        {
            Contract.Requires(streamHandle != null);
            Contract.Requires(allocator != null);

            this.streamHandle = streamHandle;
            this.writeRequestPool = new WriteRequestPool(uv_req_type.UV_WRITE);
            this.allocator = allocator;
            this.receiveBufferSizeEstimate = new ReceiveBufferSizeEstimate();
            this.bufferQueue = new BufferQueue();

            this.BufferedRead = false;
        }

        internal bool BufferedRead { get; private set; }

        internal IStream CreateStream()
        {
            this.BufferedRead = true;
            return new Stream(this.streamHandle);
        }

        internal IStream<T> CreateStream<T>() 
            where T : StreamHandle
        {
            this.BufferedRead = true;
            return new Stream<T>(this.streamHandle);
        }

        internal Action<StreamHandle, IStreamReadCompletion> ReadAction
        {
            get
            {
                return this.readAction;
            }
            set
            {
                if (this.readAction != null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(Pipeline)} channel data handler has already been registered");
                }

                this.readAction = value;
            }
        }

        internal IByteBufferAllocator Allocator => this.allocator;

        internal BufferRef AllocateReadBuffer()
        {
            ByteBuffer buffer = this.receiveBufferSizeEstimate.Allocate(this.allocator);
            Log.TraceFormat("{0} receive buffer allocated size = {1}", nameof(Pipeline), buffer.Count);

            var bufferRef = new BufferRef(buffer);
            this.bufferQueue.Enqueue(bufferRef);

            return bufferRef;
        }

        internal ByteBuffer GetBuffer(ref uv_buf_t buf)
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

        internal void OnReadCompleted(Exception exception = null)
        {
            this.bufferQueue.Clear();
            this.InvokeRead(null, 0, exception, true);
        } 

        internal void OnReadCompleted(ByteBuffer byteBuffer, int size)
        {
            Contract.Requires(byteBuffer != null);
            Contract.Requires(size >= 0);

            this.receiveBufferSizeEstimate.Record(size);
            if (size == 0)
            {
                byteBuffer.Dispose();
            }
            else
            {
                this.InvokeRead(byteBuffer, size);
            }
        }

        void InvokeRead(ByteBuffer byteBuffer, int size, Exception error = null, bool completed = false)
        {
            var completion = new StreamReadCompletion(
                byteBuffer?.ToReadableBuffer(size) ?? ReadableBuffer.Empty, 
                error, 
                completed);
            try
            {
                this.ReadAction?.Invoke(this.streamHandle, completion);
            }
            catch (Exception exception)
            {
                Log.Warn($"{nameof(Pipeline)} Exception whilst invoking read callback.", exception);
            }
            finally
            {
                if (!this.BufferedRead)
                {
                    completion.Dispose();
                }
            }
        }

        internal void QueueWrite(BufferRef bufferRef, Action<StreamHandle, Exception> completion)
        {
            Contract.Requires(bufferRef != null);

            try
            {
                WriteRequest request = this.writeRequestPool.Take();
                Contract.Assert(request != null);

                // ReSharper disable once PossibleNullReferenceException
                request.Prepare(bufferRef, 
                    (writeRequest, exception) =>
                    {
                        this.writeRequestPool.Return(writeRequest);
                        completion?.Invoke(this.streamHandle, exception);
                    });

                this.streamHandle.WriteStream(request);
            }
            catch (Exception exception)
            {
                Log.Error($"{nameof(Pipeline)} {this.streamHandle.HandleType} faulted.", exception);
                throw;
            }
        }

        public void Dispose()
        {
            this.writeRequestPool.Dispose();
            this.bufferQueue.Dispose();
            this.readAction = null;
        }

        sealed class StreamReadCompletion : ReadCompletion, IStreamReadCompletion
        {
            internal StreamReadCompletion(ReadableBuffer data, Exception error, bool completed) 
                : base(data, error)
            {
                this.Completed = completed;
            }

            public bool Completed { get; }
        }
    }
}
