// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    /// <summary>
    /// Prepare handles will run the given callback once per loop iteration, 
    /// right before polling for i/o.
    /// </summary>
    public sealed class Prepare : WorkHandle
    {
        internal Prepare(LoopContext loop)
            : base(loop, uv_handle_type.UV_PREPARE)
        { }

        public Prepare Start(Action<Prepare> callback)
        {
            Contract.Requires(callback != null);

            this.Validate();
            this.ScheduleStart(state => callback.Invoke((Prepare)state));
            return this;
        }

        public void Stop() => this.StopHandle();

        public void CloseHandle(Action<Prepare> onClosed = null) =>
            base.CloseHandle(onClosed);
    }
}
