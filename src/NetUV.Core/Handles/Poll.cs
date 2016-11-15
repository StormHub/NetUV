// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    [Flags]
    public enum PollMask
    {
        Readable   = 1, // UV_READABLE
        Writable   = 2, // UV_WRITABLE
        Disconnect = 4  // UV_DISCONNECT
    };

    public sealed class Poll : ScheduleHandle
    {
        internal static readonly uv_poll_cb PollCallback = OnPollCallback;
        Action<Poll, PollMask> pollCallback;

        internal Poll(LoopContext loop, int fd)
            : base(loop, uv_handle_type.UV_POLL, fd)
        { }

        public Poll Start(PollMask eventMask, Action<Poll, PollMask> callback)
        {
            Contract.Requires(callback != null);

            this.Validate();
            this.pollCallback = callback;
            NativeMethods.PollStart(this.InternalHandle, eventMask);

            return this;
        }

        void OnPollCallback(int status, int events)
        {
            Log.TraceFormat("{0} {1} callback", this.HandleType, this.InternalHandle);
            try
            {
                if (status < 0)
                {
                    OperationException error = NativeMethods.CreateError((uv_err_code)status);
                    throw error;
                }

                this.pollCallback?.Invoke(this, (PollMask)events);
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} {this.InternalHandle} callback error.", exception);
                throw;
            }
        }

        static void OnPollCallback(IntPtr handle, int status, int events)
        {
            var poll = HandleContext.GetTarget<Poll>(handle);
            poll?.OnPollCallback(status, events);
        }

        public void Stop() => this.StopHandle();

        protected override void Close() => this.pollCallback = null;

        public void CloseHandle(Action<Poll> callback = null) =>
            base.CloseHandle(state => callback?.Invoke((Poll)state));
    }
}
