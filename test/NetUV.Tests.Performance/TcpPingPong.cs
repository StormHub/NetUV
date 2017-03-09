// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;

    sealed class TcpPingPong : IDisposable
    {
        const string PingMessage = "PING";
        const char SplitToken = '\n';
        const int DurationInMilliseconds = 5000;

        WritableBuffer dataBuffer;
        EchoServer server;
        Loop loop;
        IStream<Tcp> stream;

        long startTime;
        int pongs;
        int state;

        public TcpPingPong()
        {
            byte[] content = Encoding.UTF8.GetBytes(PingMessage + SplitToken);
            this.dataBuffer = WritableBuffer.From(content);
        }

        public void Run()
        {
            this.pongs = 0;
            this.state = 0;
            this.server = new EchoServer(HandleType.Tcp);

            this.loop = this.server.Loop;
            this.StartClient();
        }

        void StartClient()
        {
            Tcp tcp = this.loop
                .CreateTcp()
                .ConnectTo(TestHelper.LoopbackEndPoint, this.OnConnected);

            this.startTime = this.loop.Now;
            this.loop.RunDefault();

            long count = (long)Math.Floor((1000d * this.pongs) / DurationInMilliseconds);
            Console.WriteLine($"Tcp ping pong : {TestHelper.Format(count)} roundtrips/s");

            tcp.Dispose();
        }

        void OnConnected(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Tcp ping pong : client connection failed, error {error}.");
                tcp.CloseHandle(OnClose);
            }
            else
            {
                this.stream = tcp.TcpStream();
                this.stream.Subscribe(this.OnNext, OnError, OnComplete);

                // Sending the first ping
                this.stream.Write(this.dataBuffer, OnWriteCompleted);
            }
        }

        static void OnWriteCompleted(IStream<Tcp> stream, Exception error)
        {
            if (error == null)
            {
                return;
            }

            Console.WriteLine($"Tcp ping pong : failed, error {error}.");
            stream.Handle.CloseHandle(OnClose);
        }

        void OnNext(IStream<Tcp> tcp, ReadableBuffer data)
        {
            if (data.Count == 0)
            {
                return;
            }

            string message = data.ReadString(data.Count, Encoding.UTF8);
            foreach (char token in message)
            {
                if (token == SplitToken)
                {
                    this.state = 0;
                }
                else
                {
                    if (token != PingMessage[this.state])
                    {
                        Console.WriteLine($"Tcp ping pong : failed, wrong message token received {token}.");
                        this.stream.Dispose();
                        return;
                    }

                    this.state++;
                }

                if (this.state == 0)
                {
                    this.pongs++;
                    long duration = this.loop.Now - this.startTime;

                    if (duration > DurationInMilliseconds)
                    {
                        this.stream.Handle.CloseHandle(OnClose);
                        this.server.CloseServer();
                    }
                    else
                    {
                        this.stream.Write(this.dataBuffer, OnWriteCompleted);
                    }
                }
            }
        }

        static void OnError(IStream<Tcp> stream, Exception exception) =>
            Console.WriteLine($"Tcp ping pong read error {exception}");

        static void OnComplete(IStream<Tcp> stream) => stream.Handle.CloseHandle(OnClose);

        static void OnClose(ScheduleHandle handle) => handle.Dispose();

        public void Dispose()
        {
            this.dataBuffer.Dispose();
            this.server = null;
            this.loop = null;
        }
    }
}
