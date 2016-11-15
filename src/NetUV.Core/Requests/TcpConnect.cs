// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Requests
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;

    sealed class TcpConnect : IDisposable
    {
        readonly WatcherRequest watcherRequest;
        Action<Tcp, Exception> connectedAction;

        public TcpConnect(Tcp tcp, IPEndPoint remoteEndPoint, Action<Tcp, Exception> connectedAction)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(connectedAction != null);

            tcp.Validate();

            this.Tcp = tcp;
            this.connectedAction = connectedAction;
            this.watcherRequest = new WatcherRequest(
                uv_req_type.UV_CONNECT,
                this.OnConnected,
                h => NativeMethods.TcpConnect(h, tcp.InternalHandle, remoteEndPoint));
        }

        internal Tcp Tcp { get; private set; }

        void OnConnected(WatcherRequest request, Exception error)
        {
            if (this.Tcp == null 
                || this.connectedAction == null)
            {
                throw new ObjectDisposedException($"{nameof(TcpConnect)} has already been disposed.");
            }

            try
            {
                if (error == null)
                {
                    this.Tcp.ReadStart();
                }

                this.connectedAction(this.Tcp, error);
            }
            finally
            {
                this.Dispose();
            }
        }

        public void Dispose()
        {
            this.Tcp = null;
            this.connectedAction = null;
            this.watcherRequest.Dispose();
        }
    }
}
