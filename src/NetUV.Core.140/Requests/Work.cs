// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;

    public sealed class Work : ScheduleRequest
    {
        internal static readonly uv_work_cb WorkCallback = OnWorkCallback;
        internal static readonly uv_watcher_cb AfterWorkCallback = OnAfterWorkCallback;

        readonly RequestContext handle;

        Action<Work> workCallback;
        Action<Work> afterWorkCallback;

        internal Work(
            LoopContext loop, 
            Action<Work> workCallback, 
            Action<Work> afterWorkCallback)
            : base(uv_req_type.UV_WORK)
        {
            Contract.Requires(loop != null);
            Contract.Requires(workCallback != null);

            this.workCallback = workCallback;
            this.afterWorkCallback = afterWorkCallback;

            try
            {
                this.handle = new RequestContext(uv_req_type.UV_WORK, 0, this);
                NativeMethods.QueueWork(loop.Handle, this.handle.Handle);
            }
            catch
            {
                this.handle.Dispose();
                throw;
            }
        }

        public bool TryCancel() => this.Cancel();

        internal override IntPtr InternalHandle => this.handle.Handle;

        void OnWorkCallback()
        {
            try
            {
                this.workCallback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.RequestType} work callback error.", exception);
                throw;
            }
        }

        static void OnWorkCallback(IntPtr handle)
        {
            var request = RequestContext.GetTarget<Work>(handle);
            request?.OnWorkCallback();
        }

        void OnAfterWorkCallback()
        {
            try
            {
                this.afterWorkCallback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.RequestType} after callback error", exception);
                throw;
            }
        }

        static void OnAfterWorkCallback(IntPtr handle, int status)
        {
            var request = RequestContext.GetTarget<Work>(handle);
            request?.OnAfterWorkCallback();
        }

        protected override void Close()
        {
            this.workCallback = null;
            this.afterWorkCallback = null;
            this.handle.Dispose();
        }
    }
}
