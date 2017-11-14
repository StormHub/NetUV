// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using NetUV.Core.Buffers;
    using NetUV.Core.Native;

    abstract class WriteBufferRequest : ScheduleRequest
    {
        internal static readonly uv_watcher_cb WriteCallback = OnWriteCallback;

        readonly RequestContext handle;
        Action<WriteBufferRequest, Exception> completion;
        WriteBufferRef bufferRef;

        protected WriteBufferRequest(uv_req_type requestType)
            : base(requestType)
        {
            Contract.Requires(
                requestType == uv_req_type.UV_WRITE
                || requestType == uv_req_type.UV_UDP_SEND);

            this.handle = new RequestContext(requestType, 0, this);
            this.bufferRef = null;
        }

        internal override IntPtr InternalHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.handle.Handle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Prepare(WriteBufferRef writeBufferRef, Action<WriteBufferRequest, Exception> callback)
        {
            Debug.Assert(writeBufferRef != null && callback != null);

            if (this.bufferRef != null 
                || !this.handle.IsValid)
            {
                ThrowHelper.ThrowInvalidOperationException($"{nameof(WriteRequest)} status is invalid.");
            }

            this.completion = callback;
            this.bufferRef = writeBufferRef;
            this.bufferRef.Prepare();
        }

        internal ref uv_buf_t[] Bufs => ref this.bufferRef.Bufs;

        internal ref int Size => ref this.bufferRef.Size;

        protected virtual void Release()
        {
            WriteBufferRef buf = this.bufferRef;
            this.bufferRef = null;
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

            Action<WriteBufferRequest, Exception> callback = this.completion;
            this.Release();
            callback?.Invoke(this, error);
        }

        static void OnWriteCallback(IntPtr handle, int status)
        {
            var request = RequestContext.GetTarget<WriteBufferRequest>(handle);
            request.OnWriteCallback(status);
        }

        protected override void Close()
        {
            this.Release();
            this.handle.Dispose();
        }
    }
}
