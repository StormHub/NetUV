// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace EchoServer
{
    using System;
    using System.Net;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    sealed class UdpServer : IServer
    {
        readonly Loop loop;
        readonly IPEndPoint endPoint;
        Udp server;

        public UdpServer(int port)
        {
            this.loop = new Loop();
            this.endPoint = new IPEndPoint(IPAddress.Any, port);
        }

        public void Run()
        {
            this.server = this.loop
                .CreateUdp()
                .Bind(this.endPoint)
                .MulticastLoopback(true)
                .ReceiveStart(this.OnReceive);
            Console.WriteLine($"{nameof(UdpServer)} started on {this.endPoint}");

            this.loop.RunDefault();
            Console.WriteLine($"{nameof(UdpServer)} loop completed.");
        }

        void OnReceive(Udp udp, IDatagramReadCompletion completion)
        {
            if (completion.Error != null)
            {
                Console.WriteLine($"{nameof(UdpServer)} receive error {completion.Error}");
                udp.CloseHandle(OnClosed);
                return;
            }

            IPEndPoint remoteEndPoint = completion.RemoteEndPoint;
            ReadableBuffer data = completion.Data;
            string message = data.ReadString(Encoding.UTF8);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Console.WriteLine($"{nameof(UdpServer)} received : {message} from {remoteEndPoint}");
            if (message.StartsWith("QS"))
            {
                Console.WriteLine($"{nameof(UdpServer)} shutting down.");
                this.server.Dispose();
            }
            else
            {
                Console.WriteLine($"{nameof(UdpServer)} sending echo back to {remoteEndPoint}.");
                WritableBuffer buffer = udp.Allocate();
                buffer.WriteString($"ECHO [{message}]", Encoding.UTF8);
                udp.QueueSend(buffer, remoteEndPoint, (handle, exception) => 
                {
                    OnSendCompleted(handle, exception);
                    buffer.Dispose();
                });
            }
        }

        static void OnSendCompleted(Udp udp, Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"{nameof(UdpServer)} send error {exception}");
                udp.CloseHandle(OnClosed);
            }
        }

        static void OnClosed(Udp handle) => handle.Dispose();

        public void Dispose() => this.loop.Dispose();
    }
}
