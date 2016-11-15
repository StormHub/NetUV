// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    public interface IStream : IObservable<ReadableBuffer>, IDisposable
    {
        StreamHandle Handle { get; }

        WritableBuffer Allocate(int size);

        void Write(WritableBuffer buffer, Action<IStream, Exception> completion);

        void Shutdown(Action<IStream, Exception> completion = null);
    }

    public interface IStream<out T> : IObservable<ReadableBuffer>, IDisposable 
        where T : StreamHandle
    {
        T Handle { get; }

        WritableBuffer Allocate(int size);

        void Write(WritableBuffer buffer, Action<IStream<T>, Exception> completion);

        void Shutdown(Action<IStream<T>, Exception> completion = null);
    }
}
