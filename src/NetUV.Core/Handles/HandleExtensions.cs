// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Handles
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using NetUV.Core.Requests;

    public static class HandleExtensions
    {
        public static Pipe Listen(this Pipe pipe,
            string name,
            Action<Pipe, Exception> onConnection,
            int backlog = ServerStream.DefaultBacklog)
        {
            Contract.Requires(pipe != null);
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(onConnection != null);
            Contract.Requires(backlog > 0);

            pipe.Bind(name);
            pipe.Listen(onConnection, backlog);

            return pipe;
        }

        public static Pipe ConnectTo(this Pipe pipe, 
            string remoteName, 
            Action<Pipe, Exception> connectionHandler)
        {
            Contract.Requires(pipe != null);
            Contract.Requires(!string.IsNullOrEmpty(remoteName));
            Contract.Requires(connectionHandler != null);

            PipeConnect request = null;
            try
            {
                request = new PipeConnect(pipe, remoteName, connectionHandler);
            }
            catch (Exception)
            {
                request?.Dispose();
                throw;
            }

            return pipe;
        }

        public static Tcp Listen(this Tcp tcp,
            IPEndPoint localEndPoint,
            Action<Tcp, Exception> onConnection,
            int backlog = ServerStream.DefaultBacklog,
            bool dualStack = false)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(localEndPoint != null);
            Contract.Requires(onConnection != null);

            tcp.Bind(localEndPoint, dualStack);
            tcp.Listen(onConnection, backlog);

            return tcp;
        }

        public static Tcp ConnectTo(this Tcp tcp,
            IPEndPoint localEndPoint, 
            IPEndPoint remoteEndPoint,
            Action<Tcp, Exception> connectedHandler,
            bool dualStack = false)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(localEndPoint != null);
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(connectedHandler != null);

            tcp.Bind(localEndPoint, dualStack);
            tcp.ConnectTo(remoteEndPoint, connectedHandler);

            return tcp;
        }

        public static Tcp ConnectTo(this Tcp tcp,
            IPEndPoint remoteEndPoint,
            Action<Tcp, Exception> connectedHandler,
            bool dualStack = false)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(remoteEndPoint != null);
            Contract.Requires(connectedHandler != null);

            TcpConnect request = null;
            try
            {
                request = new TcpConnect(tcp, remoteEndPoint, connectedHandler);
            }
            catch (Exception)
            {
                request?.Dispose();
                throw;
            }

            return tcp;
        }

        public static Tcp Bind(this Tcp tcp, 
            IPEndPoint localEndPoint,
            Action<Tcp, IStreamReadCompletion> onRead, 
            bool dualStack = false)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(localEndPoint != null);
            Contract.Requires(onRead != null);

            tcp.Bind(localEndPoint, dualStack);
            tcp.OnRead(onRead);

            return tcp;
        }

        public static Udp ReceiveStart(this Udp udp,
            IPEndPoint localEndPoint,
            Action<Udp, IDatagramReadCompletion> receiveAction,
            bool dualStack = false)
        {
            Contract.Requires(udp != null);
            Contract.Requires(localEndPoint != null);
            Contract.Requires(receiveAction != null);

            udp.Bind(localEndPoint, dualStack);
            udp.OnReceive(receiveAction);
            udp.ReceiveStart();

            return udp;
        }

        public static Udp ReceiveStart(this Udp udp, 
            Action<Udp, IDatagramReadCompletion> receiveAction)
        {
            Contract.Requires(udp != null);
            Contract.Requires(receiveAction != null);

            udp.OnReceive(receiveAction);
            udp.ReceiveStart();

            return udp;
        }
    }
}
