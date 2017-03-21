// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        internal ReadCompletion(ref ReadableBuffer data, Exception error)
        {
            this.Data = data;
            this.Error = error;
        }

        public ReadableBuffer Data { get; }

        public Exception Error { get; private set; }

        public void Dispose()
        {
            this.Data.Dispose();
            this.Error = null;
        }
    }
}
