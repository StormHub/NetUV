// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetUV.Core.Logging;
    using NetUV.Core.Native;

    public sealed class StreamListener<T> : IDisposable
        where T : StreamHandle
    {
        static readonly ILog Log = LogFactory.ForContext($"StreamListener:{typeof(T).Name}");

        readonly ServerStream streamHandle;
        GCHandle gcHandle;
        Action<T> closeCallback;

        internal StreamListener(ServerStream streamHandle, Action<T, Exception> handler)
        {
            Contract.Requires(streamHandle != null);
            Contract.Requires(handler != null);

            streamHandle.Validate();
            streamHandle.ConnectionHandler = (handle, exception) => handler((T)handle, exception);
            this.gcHandle = GCHandle.Alloc(this, GCHandleType.Normal);

            this.streamHandle = streamHandle;
        }

        internal void Listen(int backlog)
        {
            if (!this.gcHandle.IsAllocated)
            {
                throw new ObjectDisposedException(
                    $"The stream {this.streamHandle.HandleType} is already been disposed.");
            }

            try
            {
                NativeMethods.StreamListen(this.streamHandle.InternalHandle, backlog, ServerStream.ConnectionCallback);
                Log.DebugFormat("Stream {0} {1} listening, backlog = {2}", typeof(T).Name, this.streamHandle.InternalHandle, backlog);
            }
            catch
            {
                this.Dispose();
                throw;
            }
        }

        public void Close(Action<T> callback)
        {
            Contract.Requires(callback != null);

            this.closeCallback = callback;
            this.Dispose();
        }

        void OnClosed(ScheduleHandle stream)
        {
            if (this.gcHandle.IsAllocated)
            {
                this.gcHandle.Free();
            }
            try
            {
                this.closeCallback?.Invoke((T)stream);
            }
            finally 
            {
                this.streamHandle.Dispose();
                this.closeCallback = null;
            }
        }

        public void Dispose()
        {
            this.streamHandle.ConnectionHandler = null;
            this.streamHandle.CloseHandle(this.OnClosed);
        }
    }
}
