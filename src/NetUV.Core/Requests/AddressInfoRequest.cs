// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Runtime.CompilerServices;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;

    public struct AddressInfo
    {
        internal AddressInfo(IPHostEntry hostEntry, Exception error)
        {
            this.HostEntry = hostEntry;
            this.Error = error;
        }

        public IPHostEntry HostEntry { get; }

        public Exception Error { get; }
    }

    public sealed class AddressInfoRequest : ScheduleRequest
    {
        internal static readonly uv_getaddrinfo_cb AddressInfoCallback = OnAddressInfoCallback;
        readonly RequestContext handle;
        Action<AddressInfoRequest, AddressInfo> requestCallback;

        internal unsafe AddressInfoRequest(LoopContext loop)
            : base(uv_req_type.UV_GETADDRINFO)
        {
            Contract.Requires(loop != null);

            int size = NativeMethods.GetSize(uv_req_type.UV_GETADDRINFO);
            Contract.Assert(size > 0);

            this.handle = new RequestContext(this.RequestType, size, this);

            // Loop handle
            ((uv_getaddrinfo_t*)this.handle.Handle)->loop = loop.Handle;
        }

        internal override IntPtr InternalHandle => this.handle.Handle;

        public unsafe AddressInfoRequest Start(
            string node, 
            string service, 
            Action<AddressInfoRequest, AddressInfo> callback)
        {
            Contract.Requires(!string.IsNullOrEmpty(node) 
                || !string.IsNullOrEmpty(service));
            Contract.Requires(callback != null);
            this.handle.Validate();

            this.requestCallback = callback;

            IntPtr internalHandle = this.InternalHandle;
            IntPtr loopHandle = ((uv_getaddrinfo_t*)internalHandle)->loop;
            NativeMethods.GetAddressInfo(
                loopHandle, 
                internalHandle, 
                node, 
                service, 
                AddressInfoCallback);

            return this;
        }

        public bool TryCancel() => this.Cancel();

        void OnAddressInfoCallback(int status, ref addrinfo res)
        {
            OperationException error = null;
            IPHostEntry hostEntry = null;
            if (status < 0)
            {
                error = NativeMethods.CreateError((uv_err_code)status);
            }
            else
            {
                hostEntry = GetHostEntry(ref res);
            }

            var addressInfo = new AddressInfo(hostEntry, error);
            this.requestCallback?.Invoke(this, addressInfo);
        }

        static unsafe IPHostEntry GetHostEntry(ref addrinfo res)
        {
            var hostEntry = new IPHostEntry();

            try
            {
                hostEntry.HostName = res.GetCanonName();
                var addressList = new List<IPAddress>();

                addrinfo info = res;
                while (true)
                {
                    IPAddress address = info.GetAddress();
                    if (address != null)
                    {
                        addressList.Add(address);
                    }

                    IntPtr next = info.ai_next;
                    if (next == IntPtr.Zero)
                    {
                        break;
                    }

                    info = Unsafe.Read<addrinfo>((void*)next);
                }

                hostEntry.AddressList = addressList.ToArray();
            }
            finally 
            {
                NativeMethods.FreeAddressInfo(ref res);
            }

            return hostEntry;
        }

        static void OnAddressInfoCallback(IntPtr req, int status, ref addrinfo res)
        {
            var addressInfo = RequestContext.GetTarget<AddressInfoRequest>(req);
            addressInfo?.OnAddressInfoCallback(status, ref res);
        }

        protected override void Close()
        {
            this.requestCallback = null;
            this.handle.Dispose();
        } 
    }
}
