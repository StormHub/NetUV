// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    /// <summary>
    /// Idle handles will run the given callback once per loop iteration, 
    /// right before the uv_prepare_t handles
    /// </summary>
    public sealed class Idle : WorkHandle
    {
        internal Idle(LoopContext loop)
            : base(loop, uv_handle_type.UV_IDLE)
        { }

        public Idle Start(Action<Idle> callback)
        {
            Contract.Requires(callback != null);

            this.Validate();
            this.ScheduleStart(state => callback.Invoke((Idle)state));

            return this;
        }

        public void Stop() => this.StopHandle();

        public void CloseHandle(Action<Idle> callback = null) =>
            base.CloseHandle(callback);
    }
}
