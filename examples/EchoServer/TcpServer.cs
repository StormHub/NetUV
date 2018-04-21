// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace EchoServer
{
    using System;
    using System.Net;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    sealed class TcpServer : IServer
    {
        readonly Loop loop;
        readonly IPEndPoint endPoint;
        Tcp server;

        public TcpServer(int port)
        {
            this.loop = new Loop();
            this.endPoint = new IPEndPoint(IPAddress.Loopback, port);
        }

        public void Run()
        {
            this.server = this.loop
                .CreateTcp()
                .SimultaneousAccepts(true)
                .Listen(this.endPoint, this.OnConnection);
            Console.WriteLine($"{nameof(TcpServer)} started on {this.endPoint}.");

            this.loop.RunDefault();
            Console.WriteLine($"{nameof(TcpServer)} loop completed.");
        }

        void OnConnection(Tcp client, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{nameof(TcpServer)} client connection failed {error}");
                client.CloseHandle(OnClosed);
                return;
            }
            client.OnRead(this.OnAccept, OnError);
        }

        void OnAccept(Tcp client, ReadableBuffer data)
        {
            string message = data.ReadString(Encoding.UTF8);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Console.WriteLine($"{nameof(TcpServer)} received : {message}");

            //
            // Scan for the letter Q which signals that we should quit the server.
            // If we get QS it means close the stream.
            //
            if (message.StartsWith("Q"))
            {
                Console.WriteLine($"{nameof(TcpServer)} closing client.");
                client.Dispose();

                if (!message.EndsWith("QS"))
                {
                    return;
                }

                Console.WriteLine($"{nameof(TcpServer)} shutting down.");
                this.server.Dispose();
            }
            else
            {
                Console.WriteLine($"{nameof(TcpServer)} sending echo back.");

                WritableBuffer buffer = client.Allocate();
                buffer.WriteString($"ECHO [{message}]", Encoding.UTF8);
                client.QueueWriteStream(buffer, (handle, exception) =>
                {
                    buffer.Dispose();
                    OnWriteCompleted(handle, exception);
                });
            }
        }

        static void OnWriteCompleted(Tcp handle, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{nameof(TcpServer)} server write error {error}");
                handle.CloseHandle(OnClosed);
            }
        }

        static void OnError(Tcp handle, Exception error)
            => Console.WriteLine($"{nameof(TcpServer)} read error {error}");

        static void OnClosed(Tcp handle) => handle.Dispose();

        public void Dispose() => this.server.Dispose();
    }
}
