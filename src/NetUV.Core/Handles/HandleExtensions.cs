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
        internal const int DefaultBacklog = 128;

        public static StreamListener<Pipe> Listen(this Pipe pipe, 
            Action<Pipe, Exception> connectionHandler,
            int backlog = DefaultBacklog)
        {
            Contract.Requires(pipe != null);
            Contract.Requires(connectionHandler != null);
            Contract.Requires(backlog > 0);

            var listener = new StreamListener<Pipe>(pipe, connectionHandler);
            listener.Listen(backlog);

            return listener;
        }

        public static StreamListener<Pipe> Listen(this Pipe pipe,
            string name,
            Action<Pipe, Exception> connectionHandler,
            int backlog = DefaultBacklog)
        {
            Contract.Requires(pipe != null);
            Contract.Requires(!string.IsNullOrEmpty(name));
            Contract.Requires(connectionHandler != null);
            Contract.Requires(backlog > 0);

            pipe.Bind(name);
            var listener = new StreamListener<Pipe>(pipe, connectionHandler);
            listener.Listen(backlog);

            return listener;
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
            catch (Exception exception)
            {
                ScheduleHandle.Log.Error($"{pipe.HandleType} {pipe.InternalHandle} Failed to connect to {remoteName}", exception);
                request?.Dispose();
                throw;
            }

            return pipe;
        }

        public static StreamListener<Tcp> Listen(this Tcp tcp, 
            Action<Tcp, Exception> connectionHandler,
            int backlog = DefaultBacklog)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(connectionHandler != null);
            Contract.Requires(backlog > 0);

            var listener = new StreamListener<Tcp>(tcp, connectionHandler);
            listener.Listen(backlog);

            return listener;
        }

        public static StreamListener<Tcp> Listen(this Tcp tcp,
            IPEndPoint localEndPoint,
            Action<Tcp, Exception> connectionHandler,
            int backlog = DefaultBacklog,
            bool dualStack = false)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(localEndPoint != null);
            Contract.Requires(connectionHandler != null);

            tcp.Bind(localEndPoint, dualStack);
            var listener = new StreamListener<Tcp>(tcp, connectionHandler);
            listener.Listen(backlog);

            return listener;
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
            catch (Exception exception)
            {
                ScheduleHandle.Log.Error($"{tcp.HandleType} {tcp.InternalHandle} Failed to connect to {remoteEndPoint}", exception);
                request?.Dispose();
                throw;
            }

            return tcp;
        }

        public static Tcp Bind(this Tcp tcp, 
            IPEndPoint localEndPoint,
            Action<Tcp, IStreamReadCompletion> readAction, 
            bool dualStack = false)
        {
            Contract.Requires(tcp != null);
            Contract.Requires(localEndPoint != null);
            Contract.Requires(readAction != null);

            tcp.Bind(localEndPoint, dualStack);
            tcp.RegisterRead(readAction);

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
            udp.RegisterReceiveAction(receiveAction);
            udp.ReceiveStart();

            return udp;
        }


        public static Udp ReceiveStart(this Udp udp, 
            Action<Udp, IDatagramReadCompletion> receiveAction)
        {
            Contract.Requires(udp != null);
            Contract.Requires(receiveAction != null);

            udp.RegisterReceiveAction(receiveAction);
            udp.ReceiveStart();

            return udp;
        }
    }
}
