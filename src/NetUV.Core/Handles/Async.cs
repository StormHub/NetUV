// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Concurrency;
    using NetUV.Core.Native;

    /// <summary>
    /// Async handles allow the user to “wakeup” the event loop and get 
    /// a callback called from another thread.
    /// </summary>
    public sealed class Async : WorkHandle
    {
        readonly Gate gate;
        volatile bool closeScheduled;

        internal Async(LoopContext loop, Action<Async> callback)
            : base(loop, uv_handle_type.UV_ASYNC)
        {
            Contract.Requires(callback != null);

            this.Callback = state => callback.Invoke((Async)state);
            this.gate = new Gate();
            this.closeScheduled = false;
        }

        public Async Send()
        {
            IDisposable guard = null;
            try
            {
                guard = this.gate.TryAquire();
                if (guard != null 
                    && !this.closeScheduled)
                {
                    NativeMethods.Send(this.InternalHandle);
                }
            }
            finally
            {
                guard?.Dispose();
            }

            return this;
        }

        protected override void ScheduleClose(Action<ScheduleHandle> handler = null)
        {
            using (this.gate.Aquire())
            {
                this.closeScheduled = true;
                base.ScheduleClose(handler);
            }
        }

        public void CloseHandle(Action<Async> onClosed = null) => 
            base.CloseHandle(onClosed);
    }
}
