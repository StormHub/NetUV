// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    public sealed class WatcherRequest : ScheduleRequest
    {
        internal static readonly uv_watcher_cb WatcherCallback = OnWatcherCallback;
        readonly RequestContext handle;
        readonly bool closeOnCallback;
        Action<WatcherRequest, Exception> watcherCallback;

        internal WatcherRequest(
            uv_req_type requestType,
            Action<WatcherRequest, Exception> watcherCallback,
            int size = 0,
            bool closeOnCallback = false)
            : base(requestType)
        {
            Contract.Requires(size >= 0);

            this.watcherCallback = watcherCallback;
            this.closeOnCallback = closeOnCallback;
            this.handle = new RequestContext(requestType, size, this);
        }

        internal WatcherRequest(
            uv_req_type requestType,
            Action<WatcherRequest, Exception> watcherCallback,
            Action<IntPtr> initializer,
            bool closeOnCallback = false)
            : base(requestType)
        {
            Contract.Requires(initializer != null);

            this.watcherCallback = watcherCallback;
            this.closeOnCallback = closeOnCallback;
            this.handle = new RequestContext(requestType, initializer, this);
        }

        internal override IntPtr InternalHandle => this.handle.Handle;

        void OnWatcherCallback(OperationException error)
        {
            try
            {
                if (error != null)
                {
                    Log.Error($"{this.RequestType} {this.InternalHandle} error : {error.ErrorCode} {error.ErrorName}.", error);
                }

                this.watcherCallback?.Invoke(this, error);

                if (this.closeOnCallback)
                {
                    this.Dispose();
                }
            }
            catch (Exception exception)
            {
                Log.Error($"{this.RequestType} {nameof(OnWatcherCallback)} error.", exception);
                throw;
            }
        }

        static void OnWatcherCallback(IntPtr handle, int status)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var request = RequestContext.GetTarget<WatcherRequest>(handle);
            OperationException error = null;
            if (status < 0)
            {
                error = NativeMethods.CreateError((uv_err_code)status);
            }

            request?.OnWatcherCallback(error);
        }

        protected override void Close()
        {
            this.watcherCallback = null;
            this.handle.Dispose();
        } 
    }
}
