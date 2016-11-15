// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;


    sealed class StreamObserver : IObserver<ReadableBuffer>
    {
        readonly IStream stream;
        readonly Action<IStream, ReadableBuffer> onNext;
        readonly Action<IStream, Exception> onError;
        readonly Action<IStream> onComplete;

        public StreamObserver(
            IStream stream,
            Action<IStream, ReadableBuffer> onNext,
            Action<IStream, Exception> onError,
            Action<IStream> onComplete)
        {
            Contract.Requires(stream != null);
            Contract.Requires(onNext != null);

            this.stream = stream;

            this.onNext = onNext;
            this.onError = onError;
            this.onComplete = onComplete;
        }

        public void OnNext(ReadableBuffer value)
        {
            try
            {
                this.onNext(this.stream, value);
            }
            catch (Exception exception)
            {
                this.OnError(exception);
            }
        }

        public void OnError(Exception error)
        {
            if (this.onError != null)
            {
                this.onError(this.stream, error);
            }
            else
            {
                throw error;
            }
        }

        public void OnCompleted() => this.onComplete?.Invoke(this.stream);
    }

    sealed class StreamObserver<T> : IObserver<ReadableBuffer> 
        where T : StreamHandle
    {
        readonly IStream<T> stream;
        readonly Action<IStream<T>, ReadableBuffer> onNext;
        readonly Action<IStream<T>, Exception> onError;
        readonly Action<IStream<T>> onComplete;

        public StreamObserver(
            IStream<T> stream, 
            Action<IStream<T>, ReadableBuffer> onNext, 
            Action<IStream<T>, Exception> onError, 
            Action<IStream<T>> onComplete)
        {
            Contract.Requires(stream != null);
            Contract.Requires(onNext != null);

            this.stream = stream;

            this.onNext = onNext;
            this.onError = onError;
            this.onComplete = onComplete;
        }

        public void OnNext(ReadableBuffer value)
        {
            try
            {
                this.onNext(this.stream, value);
            }
            catch (Exception exception)
            {
                this.OnError(exception);
            }
        }

        public void OnError(Exception error)
        {
            if (this.onError != null)
            {
                this.onError(this.stream, error);
            }
            else
            {
                throw error;
            }
        }

        public void OnCompleted() => this.onComplete?.Invoke(this.stream);
    }
}
