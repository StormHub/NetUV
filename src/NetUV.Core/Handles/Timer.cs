// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    /// <summary>
    /// Timer handles are used to schedule callbacks to be called in the future.
    /// </summary>
    public sealed class Timer : WorkHandle
    {
        internal Timer(LoopContext loop)
            : base(loop, uv_handle_type.UV_TIMER)
        { }

        public Timer Start(Action<Timer> callback, long timeout, long repeat)
        {
            Contract.Requires(callback != null);
            Contract.Requires(timeout >= 0);
            Contract.Requires(repeat >= 0);

            this.Validate();
            this.Callback = state => callback((Timer)state);
            NativeMethods.Start(this.InternalHandle, timeout, repeat);

            return this;
        }

        public Timer SetRepeat(long repeat)
        {
            Contract.Requires(repeat >= 0);

            this.Validate();
            NativeMethods.SetTimerRepeat(this.InternalHandle, repeat);

            return this;
        }

        public long GetRepeat()
        {
            this.Validate();
            return NativeMethods.GetTimerRepeat(this.InternalHandle);
        }

        public Timer Again()
        {
            this.Validate();
            NativeMethods.Again(this.InternalHandle);

            return this;
        }

        public void Stop() => this.StopHandle();

        public void CloseHandle(Action<Timer> onClosed = null) =>
            base.CloseHandle(onClosed);
    }
}
