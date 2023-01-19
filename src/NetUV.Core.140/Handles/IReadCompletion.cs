// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace NetUV.Core.Handles
{
    using System;
    using System.Net;
    using NetUV.Core.Buffers;

    public interface IReadCompletion : IDisposable
    {
        ReadableBuffer Data { get; }

        Exception Error { get; }
    }

    public interface IStreamReadCompletion : IReadCompletion
    {
        bool Completed { get; }
    }

    public interface IDatagramReadCompletion : IReadCompletion
    {
        IPEndPoint RemoteEndPoint { get; }
    }

    class ReadCompletion : IReadCompletion
    {
        readonly ReadableBuffer readableBuffer;
        Exception error;

        internal ReadCompletion(ref ReadableBuffer data, Exception error)
        {
            this.readableBuffer = data;
            this.error = error;
        }

        public ReadableBuffer Data => this.readableBuffer;

        public Exception Error => this.error;

        public void Dispose()
        {
            IByteBuffer buffer = this.Data.Buffer;
            if (buffer.ReferenceCount > 0)
            {
                buffer.Release();
            }
            this.error = null;
        }
    }
}
