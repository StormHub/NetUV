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
        None = 0,
        Readable = 1, // UV_READABLE
        Writable   = 2, // UV_WRITABLE
        Disconnect = 4  // UV_DISCONNECT
    };

    public struct PollStatus
    {
        internal PollStatus(PollMask mask, Exception error)
        {
            this.Mask = mask;
            this.Error = error;
        }

        public PollMask Mask { get; }

        public Exception Error { get; }
    }

    public sealed class Poll : ScheduleHandle
    {
        internal static readonly uv_poll_cb PollCallback = OnPollCallback;
        Action<Poll, PollStatus> pollCallback;

        internal Poll(LoopContext loop, int fd)
            : base(loop, uv_handle_type.UV_POLL, new object[] { fd })
        { }

        internal Poll(LoopContext loop, IntPtr handle)
            : base(loop, uv_handle_type.UV_POLL, new object[] { handle })
        { }

        public IntPtr GetFileDescriptor()
        {
            this.Validate();
            return NativeMethods.GetFileDescriptor(this.InternalHandle);
        }

        public Poll Start(PollMask eventMask, Action<Poll, PollStatus> callback)
        {
            Contract.Requires(callback != null);

            this.Validate();
            this.pollCallback = callback;
            NativeMethods.PollStart(this.InternalHandle, eventMask);

            return this;
        }

        void OnPollCallback(int status, int events)
        {
            if (Log.IsTraceEnabled)
            {
                Log.TraceFormat("{0} {1} callback", this.HandleType, this.InternalHandle);
            }
            try
            {
                OperationException error = null;
                var mask = PollMask.None;
                if (status < 0)
                {
                    error = NativeMethods.CreateError((uv_err_code)status);
                }
                else
                {
                    mask = (PollMask)events;
                }

                this.pollCallback?.Invoke(this, new PollStatus(mask, error));
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

        public void CloseHandle(Action<Poll> onClosed = null)
        {
            Action<ScheduleHandle> handler = null;
            if (onClosed != null)
            {
                handler = state => onClosed((Poll)state);
            }

            base.CloseHandle(handler);
        }
    }
}
