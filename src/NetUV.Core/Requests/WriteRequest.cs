// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    sealed class WriteRequest : ScheduleRequest
    {
        internal static readonly uv_watcher_cb WriteCallback = OnWriteCallback;

        readonly RequestContext handle;
        Action<WriteRequest, Exception> completion;
        BufferRef buffer;

        internal WriteRequest(uv_req_type requestType) 
            : base(requestType)
        {
            Contract.Requires(
                requestType == uv_req_type.UV_WRITE 
                || requestType == uv_req_type.UV_UDP_SEND);

            this.handle = new RequestContext(requestType, 0, this);
            this.buffer = null;
        }

        internal override IntPtr InternalHandle => this.handle.Handle;

        internal void Prepare(BufferRef bufferRef, Action<WriteRequest, Exception> callback)
        {
            Contract.Requires(bufferRef != null);
            Contract.Requires(callback != null);

            if (this.buffer != null 
                || !this.handle.IsValid)
            {
                throw new InvalidOperationException($"{nameof(WriteRequest)} status is invalid.");
            }

            this.completion = callback;
            this.buffer = bufferRef;
        }

        internal uv_buf_t[] Bufs
        {
            get
            {
                if (this.buffer == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(WriteRequest)} buffer has not been initialized.");
                }

                return this.buffer.GetBuffer();
            }
        } 

        void Release()
        {
            BufferRef buf = this.buffer;
            this.buffer = null;
            this.completion = null;

            buf?.Dispose();
        }

        void OnWriteCallback(int status)
        {
            OperationException error = null;
            if (status < 0)
            {
                error = NativeMethods.CreateError((uv_err_code)status);
            }

            Action<WriteRequest, Exception> callback = this.completion;
            this.Release();
            callback?.Invoke(this, error);
        }

        static void OnWriteCallback(IntPtr handle, int status)
        {
            var request = RequestContext.GetTarget<WriteRequest>(handle);
            request.OnWriteCallback(status);
        }

        protected override void Close()
        {
            this.Release();
            this.handle.Dispose();
        }
    }
}
