// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    /// <summary>
    /// Check handles will run the given callback once per loop iteration, 
    /// right after polling for i/o.
    /// </summary>
    public sealed class Check : WorkHandle
    {
        internal Check(LoopContext loop)
            : base(loop, uv_handle_type.UV_CHECK)
        { }

        public Check Start(Action<Check> callback)
        {
            Contract.Requires(callback != null);

            this.Validate();
            this.ScheduleStart(state => callback.Invoke((Check)state));

            return this;
        }

        public void Stop() => this.StopHandle();

        public void CloseHandle(Action<Check> onClosed = null) =>
            base.CloseHandle(onClosed);
    }
}
