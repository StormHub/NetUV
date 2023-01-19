// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;

    sealed class StreamShutdown : IDisposable
    {
        readonly WatcherRequest watcherRequest;
        StreamHandle streamHandle;
        Action<StreamHandle, Exception> completedAction;

        internal StreamShutdown(StreamHandle streamHandle, Action<StreamHandle, Exception> completedAction)
        {
            Contract.Requires(streamHandle != null);

            streamHandle.Validate();

            this.streamHandle = streamHandle;
            this.completedAction = completedAction;

            this.watcherRequest = new WatcherRequest(
                uv_req_type.UV_SHUTDOWN,
                this.OnCompleted,
                h => NativeMethods.Shutdown(h, this.streamHandle.InternalHandle),
                closeOnCallback: true);
        }

        internal static void Completed(Action<StreamHandle, Exception> completion, StreamHandle handle, Exception error)
        {
            Contract.Requires(handle != null);

            try
            {
                completion?.Invoke(handle, error);
            }
            catch (Exception exception)
            {
                ScheduleRequest.Log.Error("UV_SHUTDOWN callback error.", exception);
            }
        }

        void OnCompleted(WatcherRequest request, Exception error)
        {
            Completed(this.completedAction, this.streamHandle, error);
            this.Dispose();
        }

        public void Dispose()
        {
            this.streamHandle = null;
            this.completedAction = null;
            this.watcherRequest.Dispose();
        }
    }
}
