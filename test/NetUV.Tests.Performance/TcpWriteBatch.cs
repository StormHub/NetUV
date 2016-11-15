// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;

    sealed class TcpWriteBatch : IDisposable
    {
        const string Content = "Hello, world.";
        const long NumberOfRequests = (1000 * 1000);

        WritableBuffer dataBuffer;
        BlackholeServer server;
        Loop loop;

        long writeCount;
        long batchWriteCommit;
        long batchWriteFinish;

        public TcpWriteBatch()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Content);
            this.dataBuffer = WritableBuffer.From(bytes);
            this.writeCount = 0;
        }

        public void Run()
        {
            this.server = new BlackholeServer();
            this.loop = this.server.Loop;
            this.StartClient();
        }

        void StartClient()
        {
            this.loop
                .CreateTcp()
                .ConnectTo(BlackholeServer.LoopbackEndPoint, this.OnConnected);

            this.loop.RunDefault();

            if (this.writeCount != NumberOfRequests)
            {
                Console.WriteLine($"Tcp write batch : failed, expecting number of writes {NumberOfRequests}.");
            }
            else
            {
                double seconds = (double)(this.batchWriteFinish - this.batchWriteCommit) / TestHelper.NanoSeconds;
                Console.WriteLine($"Tcp write batch : {TestHelper.Format(NumberOfRequests)} write requests in {TestHelper.Format(seconds)} seconds.");
            }
        }

        void OnConnected(Tcp tcp, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Tcp write batch : Write request connection failed {error}.");
                tcp.Dispose();
                return;
            }

            IStream<Tcp> stream = tcp.TcpStream();
            stream.Subscribe(OnNext, OnError);

            for (int i = 0; i < NumberOfRequests; i++)
            {
                stream.Write(this.dataBuffer, this.OnWriteComplete);
            }

            this.batchWriteCommit = this.loop.NowInHighResolution;
            stream.Shutdown(this.OnShutdown);
        }

        void OnShutdown(IStream<Tcp> stream, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Tcp write batch : shutdown error {error}.");
            }

            this.batchWriteFinish = this.loop.NowInHighResolution;
            stream.Dispose();
            this.server.Dispose();
        }

        static void OnNext(IStream<Tcp> stream, ReadableBuffer readableBuffer)
        {
            // NOP
        }

        static void OnError(IStream<Tcp> stream, Exception exception) => 
            Console.WriteLine($"Tcp write batch : read error {exception}.");

        void OnWriteComplete(IStream<Tcp> clientStream, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Tcp write batch : write error {error}.");
                clientStream.Dispose();
                return;
            }

            this.writeCount++;
        }

        public void Dispose()
        {
            this.dataBuffer.Dispose();
            this.server = null;
            this.loop = null;
        }
    }
}
