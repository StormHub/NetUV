// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;

    [Flags]
    public enum NameInfoFlags
    {
        None = 0,
        NoFullyQualifiedDomainName = 1, // NI_NOFQDN
        NumericHost = 2,                // NI_NUMERICHOST
        NameRequired = 4,               // NI_NAMEREQD
        NumericServiceAddress = 8,      // NI_NUMERICSERV
        Datagram = 16,                  // NI_DGRAM
    }

    public struct NameInfo
    {
        internal NameInfo(string hostName, string service, Exception error)
        {
            this.HostName = hostName;
            this.Service = service;
            this.Error = error;
        }

        public string HostName { get; }

        public string Service { get; }

        public Exception Error { get; }
    }

    public sealed class NameInfoRequest : ScheduleRequest
    {
        internal static readonly uv_getnameinfo_cb NameInfoCallback = OnNameInfoCallback;
        readonly RequestContext handle;
        Action<NameInfoRequest, NameInfo> requestCallback;

        internal unsafe NameInfoRequest(LoopContext loop)
            : base(uv_req_type.UV_GETNAMEINFO)
        {
            Contract.Requires(loop != null);

            int size = NativeMethods.GetSize(uv_req_type.UV_GETNAMEINFO);
            Contract.Assert(size > 0);

            this.handle = new RequestContext(this.RequestType, size, this);

            // Loop handle
            ((uv_getnameinfo_t*)this.handle.Handle)->loop = loop.Handle;
        }

        internal override IntPtr InternalHandle => this.handle.Handle;

        public unsafe NameInfoRequest Start(
            IPEndPoint endPoint,
            Action<NameInfoRequest, NameInfo> callback, 
            NameInfoFlags flags = NameInfoFlags.None)
        {
            Contract.Requires(endPoint != null);
            Contract.Requires(callback != null);

            this.handle.Validate();
            this.requestCallback = callback;

            IntPtr internalHandle = this.InternalHandle;
            IntPtr loopHandle = ((uv_getaddrinfo_t*)internalHandle)->loop;
            NativeMethods.GetNameInfo(
                loopHandle, 
                internalHandle, 
                endPoint, 
                flags, 
                NameInfoCallback);

            return this;
        }

        public bool TryCancel() => this.Cancel();

        void OnNameInfoCallback(int status, string hostname, string service)
        {
            OperationException error = null;
            if (status < 0)
            {
                error = NativeMethods.CreateError((uv_err_code)status);
            }

            var nameInfo = new NameInfo(hostname, service, error);
            this.requestCallback?.Invoke(this, nameInfo);
        }

        static void OnNameInfoCallback(IntPtr req, int status, string hostname, string service)
        {
            var nameInfo = RequestContext.GetTarget<NameInfoRequest>(req);
            nameInfo?.OnNameInfoCallback(status, hostname, service);
        }

        protected override void Close()
        {
            this.requestCallback = null;
            this.handle.Dispose();
        }
    }
}
