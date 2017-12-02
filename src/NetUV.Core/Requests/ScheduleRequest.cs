// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using NetUV.Core.Handles;
    using NetUV.Core.Logging;
    using NetUV.Core.Native;

    public abstract class ScheduleRequest : IDisposable
    {
        internal static readonly ILog Log = LogFactory.ForContext<ScheduleHandle>();

        internal ScheduleRequest(uv_req_type requestType)
        {
            this.RequestType = requestType;
        }

        public bool IsValid => this.InternalHandle != IntPtr.Zero;

        public object UserToken { get; set; }

        internal abstract IntPtr InternalHandle { get; }

        internal uv_req_type RequestType { get; }

        protected bool Cancel() => 
            this.IsValid && NativeMethods.Cancel(this.InternalHandle);

        protected abstract void Close();

        public override string ToString() =>
            $"{this.RequestType} {this.InternalHandle}";

        public void Dispose()
        {
            if (!this.IsValid)
            {
                return;
            }

            this.UserToken = null;
            this.Close();
        }
    }
}
