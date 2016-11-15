// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;

    sealed class PipeConnect : IDisposable
    {
        readonly WatcherRequest watcherRequest;
        Action<Pipe, Exception> connectedAction;

        public PipeConnect(Pipe pipe, string remoteName, Action<Pipe, Exception> connectedAction)
        {
            Contract.Requires(pipe != null);
            Contract.Requires(!string.IsNullOrEmpty(remoteName));
            Contract.Requires(connectedAction != null);

            pipe.Validate();

            this.Pipe = pipe;
            this.connectedAction = connectedAction;
            this.watcherRequest = new WatcherRequest(
                uv_req_type.UV_CONNECT,
                this.OnConnected,
                h => NativeMethods.PipeConnect(h, pipe.InternalHandle, remoteName));
        }

        internal Pipe Pipe { get; private set; }

        void OnConnected(WatcherRequest request, Exception error)
        {
            if (this.Pipe == null
                || this.connectedAction == null)
            {
                throw new ObjectDisposedException($"{nameof(PipeConnect)} has already been disposed.");
            }

            try
            {
                if (error == null)
                {
                    this.Pipe.ReadStart();
                }

                this.connectedAction(this.Pipe, error);
            }
            finally
            {
                this.Dispose();
            }
        }

        public void Dispose()
        {
            this.Pipe = null;
            this.connectedAction = null;
            this.watcherRequest.Dispose();
        }
    }
}
