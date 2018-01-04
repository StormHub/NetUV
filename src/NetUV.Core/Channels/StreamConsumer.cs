// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Channels
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    sealed class StreamConsumer<T> : IStreamConsumer<T> 
        where T : StreamHandle
    {
        readonly Action<T, ReadableBuffer> onAccept;
        readonly Action<T, Exception> onError;
        readonly Action<T> onCompleted;

        public StreamConsumer(
            Action<T, ReadableBuffer> onAccept, 
            Action<T, Exception> onError, 
            Action<T> onCompleted)
        {
            Contract.Requires(onAccept != null);
            Contract.Requires(onError != null);

            this.onAccept = onAccept;
            this.onError = onError;
            this.onCompleted = onCompleted ?? OnCompleted;
        }

        public void Consume(T stream, IStreamReadCompletion readCompletion)
        {
            try
            {
                if (readCompletion.Error != null)
                {
                    this.onError(stream, readCompletion.Error);
                }
                else
                {
                    this.onAccept(stream, readCompletion.Data);
                }

                if (readCompletion.Completed)
                {
                    this.onCompleted(stream);
                }
            }
            catch (Exception exception)
            {
                this.onError(stream, exception);
            }
        }

        static void OnCompleted(T stream) => stream.CloseHandle(OnClosed);

        static void OnClosed(StreamHandle streamHandle) => streamHandle.Dispose();
    }
}
