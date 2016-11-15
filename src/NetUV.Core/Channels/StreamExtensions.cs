// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    public static class StreamExtensions
    {
        public static IDisposable Subscribe(this IStream stream,
            Action<IStream, ReadableBuffer> onNext,
            Action<IStream, Exception> onError = null,
            Action<IStream> onCompleted = null)
        {
            Contract.Requires(stream != null);
            Contract.Requires(onNext != null);

            var observer = new StreamObserver(stream,
                onNext, onError, onCompleted);
            return stream.Subscribe(observer);
        }

        public static IDisposable Subscribe<T>(this IStream<T> stream, 
            Action<IStream<T>, ReadableBuffer> onNext, 
            Action<IStream<T>, Exception> onError = null, 
            Action<IStream<T>> onCompleted = null) 
            where T : StreamHandle
        {
            Contract.Requires(stream != null);
            Contract.Requires(onNext != null);

            var observer = new StreamObserver<T>(stream, 
                onNext, onError, onCompleted);
            return stream.Subscribe(observer);
        }
    }
}
