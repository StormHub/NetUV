// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
#if NET46
    using System.Threading;
#endif
    using NetUV.Core.Native;

    /// <summary>
    /// Async handles allow the user to “wakeup” the event loop and get 
    /// a callback called from another thread.
    /// </summary>
    public sealed class Async : WorkHandle
    {
        internal Async(LoopContext loop, Action<Async> callback)
            : base(loop, uv_handle_type.UV_ASYNC)
        {
            Contract.Requires(callback != null);

            this.Callback = state => callback.Invoke((Async)state);
        }

        public Async Send()
        {
            this.Validate();

#if NET46
            if (ExecutionContext.IsFlowSuppressed())
            {
                NativeMethods.Send(this.InternalHandle);
            }
            else
            {
                using (ExecutionContext.SuppressFlow())
                {
                    NativeMethods.Send(this.InternalHandle);
                }
            }
#else
            NativeMethods.Send(this.InternalHandle);
#endif
            return this;
        }

        public void CloseHandle(Action<Async> callback = null) => 
            base.CloseHandle(callback);
    }
}
