// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;

    public struct FSPollStatus
    {
        internal FSPollStatus(FileStatus previous, FileStatus current)
        {
            this.Previous = previous;
            this.Current = current;
        }

        public FileStatus Previous { get; }

        public FileStatus Current { get; }
    }

    public sealed class FSPoll : ScheduleHandle
    {
        internal static readonly uv_fs_poll_cb FSPollCallback = OnFSPollCallback;
        Action<FSPoll, FSPollStatus> pollCallback;

        internal FSPoll(LoopContext loop)
            : base(loop, uv_handle_type.UV_FS_POLL)
        { }

        public FSPoll Start(string path, int interval, Action<FSPoll, FSPollStatus> callback)
        {
            Contract.Requires(!string.IsNullOrEmpty(path));
            Contract.Requires(interval > 0);
            Contract.Requires(callback != null);

            this.Validate();
            this.pollCallback = callback;
            NativeMethods.FSPollStart(this.InternalHandle, path, interval);

            return this;
        }

        public string GetPath()
        {
            if (this.pollCallback == null)
            {
                throw new InvalidOperationException(
                    $"{this.HandleType} {this.InternalHandle} is not started.");
            }

            this.Validate();
            return NativeMethods.FSPollGetPath(this.InternalHandle);
        }

        void OnFSPollCallback(int status, ref uv_stat_t prev, ref uv_stat_t curr)
        {
            Log.TraceFormat("{0} {1} callback", this.HandleType, this.InternalHandle);
            try
            {
                if (status < 0)
                {
                    OperationException error = NativeMethods.CreateError((uv_err_code)status);
                    throw error;
                }

                this.pollCallback?.Invoke(this, new FSPollStatus(prev, curr));
            }
            catch (Exception exception)
            {
                Log.Error($"{this.HandleType} {this.InternalHandle} callback error.", exception);
                throw;
            }
        }

        static void OnFSPollCallback(IntPtr handle, int status, ref uv_stat_t prev, ref uv_stat_t curr)
        {
            var fsPoll = HandleContext.GetTarget<FSPoll>(handle);
            fsPoll?.OnFSPollCallback(status, ref prev, ref curr);
        }

        public void Stop() => this.StopHandle();

        protected override void Close() => this.pollCallback = null;

        public void CloseHandle(Action<FSPoll> callback = null) =>
            base.CloseHandle(state => callback?.Invoke((FSPoll)state));
    }
}
