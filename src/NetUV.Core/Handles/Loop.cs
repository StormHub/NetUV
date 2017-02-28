// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Native;
    using NetUV.Core.Requests;

    public sealed class Loop : IDisposable
    {
        readonly LoopContext handle;

        public Loop() 
        {
            this.handle = new LoopContext();
        }

        public bool IsAlive => this.handle.IsAlive;

        public long Now => this.handle.Now;

        public long NowInHighResolution => this.handle.NowInHighResolution;

        public int ActiveHandleCount() => this.handle.ActiveHandleCount();

        public void UpdateTime() => this.handle.UpdateTime();

        internal int GetBackendTimeout() => this.handle.GetBackendTimeout();

        public int RunDefault() => this.handle.Run(uv_run_mode.UV_RUN_DEFAULT);

        public int RunOnce() => this.handle.Run(uv_run_mode.UV_RUN_ONCE);

        public int RunNoWait() => this.handle.Run(uv_run_mode.UV_RUN_NOWAIT);

        public void Stop() => this.handle.Stop();

        public Udp CreateUdp()
        {
            this.handle.Validate();
            return new Udp(this.handle);
        }

        public Pipe CreatePipe(bool ipc = false)
        {
            this.handle.Validate();
            return new Pipe(this.handle, ipc);
        }

        public Tcp CreateTcp()
        {
            this.handle.Validate();
            return new Tcp(this.handle);
        }

        public Tty CreateTty(TtyType type)
        {
            this.handle.Validate();
            return new Tty(this.handle, type);
        }

        public Timer CreateTimer()
        {
            this.handle.Validate();
            return new Timer(this.handle);
        }

        public Prepare CreatePrepare()
        {
            this.handle.Validate();
            return new Prepare(this.handle);
        }

        public Check CreateCheck()
        {
            this.handle.Validate();
            return new Check(this.handle);
        }

        public Idle CreateIdle()
        {
            this.handle.Validate();
            return new Idle(this.handle);
        }

        public Async CreateAsync(Action<Async> callback)
        {
            Contract.Requires(callback != null);

            this.handle.Validate();
            return new Async(this.handle, callback);
        }

        public Poll CreatePoll(int fileDescriptor)
        {
            if (Platform.IsWindows)
            {
                throw new InvalidOperationException(
                    "Poll handle file descriptor is not supported on Windows platform");
            }
            this.handle.Validate();

            return new Poll(this.handle, fileDescriptor);
        }

        public Poll CreatePoll(IntPtr socket)
        {
            Contract.Requires(socket != IntPtr.Zero);

            if (!Platform.IsWindows)
            {
                throw new InvalidOperationException(
                    "Poll handle socket is not supported on non Windows platform");
            }
            this.handle.Validate();

            return new Poll(this.handle, socket);
        }

        public Signal CreateSignal()
        {
            this.handle.Validate();
            return new Signal(this.handle);
        }

        public FSEvent CreateFSEvent()
        {
            this.handle.Validate();
            return new FSEvent(this.handle);
        }

        public FSPoll CreateFSPoll()
        {
            this.handle.Validate();
            return new FSPoll(this.handle);
        }

        public Work CreateWorkRequest(Action<Work> workCallback, Action<Work> afterWorkCallback)
        {
            Contract.Requires(workCallback != null);

            this.handle.Validate();
            return new Work(this.handle, workCallback, afterWorkCallback);
        }

        public AddressInfoRequest CreateAddressInfoRequest()
        {
            this.handle.Validate();
            return new AddressInfoRequest(this.handle);
        }

        public NameInfoRequest CreateNameInfoRequest()
        {
            this.handle.Validate();
            return new NameInfoRequest(this.handle);
        }

        public void Dispose() => this.handle.Dispose();
    }
}
