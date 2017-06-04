// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    public class WorkHandle : ScheduleHandle
    {
        internal static readonly uv_work_cb WorkCallback = OnWorkCallback;
        protected Action<WorkHandle> Callback;

        internal WorkHandle(
            LoopContext loop, 
            uv_handle_type handleType, 
            params object[] args)
            : base(loop, handleType, args)
        { }

        protected void ScheduleStart(Action<WorkHandle> callback)
        {
            Contract.Requires(callback != null);

            this.Validate();
            this.Callback = callback;
            NativeMethods.Start(this.HandleType, this.InternalHandle);
        }

        protected override void Close() => this.Callback = null;

        void OnWorkCallback()
        {
            if (Log.IsTraceEnabled)
            {
                Log.TraceFormat("{0} {1} callback", this.HandleType, this.InternalHandle);
            }

            try
            {
                this.Callback?.Invoke(this);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} {this.InternalHandle} callback error.", exception);
                throw;
            }
        }

        static void OnWorkCallback(IntPtr handle)
        {
            var workHandle = HandleContext.GetTarget<WorkHandle>(handle);
            workHandle?.OnWorkCallback();
        }

        protected void CloseHandle<T>(Action<T> onClosed = null)
            where T : WorkHandle
        {
            Action<ScheduleHandle> handler = null;
            if (onClosed != null)
            {
                handler = state => onClosed((T)state);
            }

            base.CloseHandle(handler);
        } 
    }
}
