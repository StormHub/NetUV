// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    public sealed class Signal : ScheduleHandle
    {
        internal static readonly uv_watcher_cb SignalCallback = OnSignalCallback;
        Action<Signal, int> signalCallback;

        internal Signal(LoopContext loop)
            : base(loop, uv_handle_type.UV_SIGNAL)
        { }

        public Signal Start(int signum, Action<Signal, int> callback)
        {
            Contract.Requires(callback != null);

            this.signalCallback = callback;
            this.Validate();
            NativeMethods.SignalStart(this.InternalHandle, signum);

            return this;
        }

        void OnSignalCallback(int signum)
        {
            Log.TraceFormat("{0} {1} callback", this.HandleType, this.InternalHandle);
            try
            {
                this.signalCallback?.Invoke(this, signum);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} {this.InternalHandle} callback error.", exception);
                throw;
            }
        }

        static void OnSignalCallback(IntPtr handle, int signum)
        {
            var signal = HandleContext.GetTarget<Signal>(handle);
            signal?.OnSignalCallback(signum);
        }

        public void Stop() => this.StopHandle();

        protected override void Close() => this.signalCallback = null;

        public void CloseHandle(Action<Signal> callback = null) =>
            base.CloseHandle(state => callback?.Invoke((Signal)state));
    }
}
