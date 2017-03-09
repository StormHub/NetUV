// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Net;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;

    sealed class BlackholeServer : IDisposable
    {
        public static readonly int Port = 9089;
        public static readonly IPEndPoint LoopbackEndPoint = new IPEndPoint(IPAddress.Loopback, Port);

        readonly Tcp tcpServer;

        public BlackholeServer()
        {
            this.Loop = new Loop();
            this.tcpServer = this.Loop
                .CreateTcp()
                .NoDelay(true)
                .Listen(LoopbackEndPoint, this.OnConnection);
        }

        internal Loop Loop { get; }

        void OnConnection(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{nameof(BlackholeServer)} server client connection error {error}");
                tcp.Dispose();
                return;
            }

            tcp.TcpStream().Subscribe(OnNext, OnError, this.OnComplete);
        }

        public void Shutdown() => this.tcpServer.Shutdown(OnShutdown);

        static void OnNext(IStream<Tcp> stream, ReadableBuffer data) => data.Dispose();

        static void OnError(IStream<Tcp> stream, Exception exception) => 
            Console.WriteLine($"{nameof(BlackholeServer)} read error {exception}");

        void OnComplete(IStream<Tcp> stream)
        {
            stream.Handle.CloseHandle(OnClosed);
            this.tcpServer.Shutdown(OnShutdown);
        }

        static void OnClosed(Tcp handle) => handle.Dispose();

        static void OnShutdown(Tcp handle, Exception exception) => handle.Dispose();

        public void Dispose()
        {
            this.tcpServer.Dispose();
            this.Loop.Dispose();
        }
    }
}
