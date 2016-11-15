// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;

    sealed class EchoServer : IDisposable
    {
        const int MaximumBacklogSize = (int)SocketOptionName.MaxConnections;
        readonly IDisposable server;

        public EchoServer(HandleType handleType)
        {
            this.Loop = new Loop();

            switch (handleType)
            {
                case HandleType.Udp:
                    this.server = this.Loop
                        .CreateUdp()
                        .Bind(TestHelper.AnyEndPoint)
                        .ReceiveStart(OnReceive);
                    break;
                case HandleType.Tcp:
                    this.server = this.Loop
                        .CreateTcp()
                        .SimultaneousAccepts(true)
                        .Listen(TestHelper.LoopbackEndPoint, this.OnConnection, MaximumBacklogSize);
                    break;
                case HandleType.Pipe:
                    this.server = this.Loop
                        .CreatePipe()
                        .Listen(TestHelper.LocalPipeName, this.OnConnection);
                    break;
                default:
                    throw new InvalidOperationException($"{handleType} not supported.");
            }
        }

        internal Loop Loop { get; }

        static void OnReceive(Udp udp, IDatagramReadCompletion completion)
        {
            IPEndPoint remoteEndPoint = completion.RemoteEndPoint;
            ReadableBuffer data = completion.Data;
            if (data.Count == 0)
            {
                return;
            }

            if (completion.Error != null)
            {
                Console.WriteLine($"{nameof(EchoServer)} receive error {completion.Error}");
                udp.ReceiveStop();
                udp.Dispose();
                return;
            }

            string message = data.ReadString(data.Count, Encoding.UTF8);
            data.Dispose();

            if (remoteEndPoint == null)
            {
                return;
            }

            WritableBuffer buffer = WritableBuffer.From(Encoding.UTF8.GetBytes(message));
            udp.QueueSend(buffer, remoteEndPoint, OnSendCompleted);
        }

        static void OnSendCompleted(Udp udp, Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"{nameof(EchoServer)} send error {exception}");
            }
        }

        void OnConnection<T>(T client, Exception error) 
            where T : StreamHandle
        {
            if (error != null)
            {
                Console.WriteLine($"{nameof(EchoServer)} client connection failed, {error}");
                client.Dispose();
                return;
            }

            client.CreateStream().Subscribe(this.OnNext, OnError, OnComplete);
        }

        void OnNext(IStream stream, ReadableBuffer data)
        {
            if (data.Count == 0)
            {
                return;
            }

            string message = data.ReadString(data.Count, Encoding.UTF8);
            data.Dispose();

            //
            // Scan for the letter Q which signals that we should quit the server.
            // If we get QS it means close the stream.
            //
            if (message.StartsWith("Q"))
            {
                stream.Dispose();

                if (message.EndsWith("QS"))
                {
                    return;
                }

                this.server.Dispose();
            }
            else
            {
                WritableBuffer buffer = WritableBuffer.From(Encoding.UTF8.GetBytes(message));
                stream.Write(buffer, OnWriteCompleted);
            }
        }

        static void OnWriteCompleted(IStream stream, Exception error)
        {
            if (error == null)
            {
                return;
            }

            Console.WriteLine($"{nameof(EchoServer)} write failed, {error}");
            stream.Dispose();
        }

        static void OnError(IStream stream, Exception exception) => 
            Console.WriteLine($"{nameof(EchoServer)} read error {exception}");

        static void OnComplete(IStream stream) => 
            stream.Dispose();

        public void Dispose()
        {
            this.server.Dispose();
            this.Loop.Dispose();
        }
    }
}
