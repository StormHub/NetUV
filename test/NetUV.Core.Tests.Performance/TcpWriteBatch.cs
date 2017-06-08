// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    sealed class TcpWriteBatch : IDisposable
    {
        const string Content = "Hello, world.";
        const long NumberOfRequests = (1000 * 1000);

        readonly byte[] content;
        BlackholeServer server;
        Loop loop;

        long writeCount;
        long batchWriteCommit;
        long batchWriteFinish;

        public TcpWriteBatch()
        {
            this.content = Encoding.UTF8.GetBytes(Content);
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
                tcp.CloseHandle(OnClosed);
                return;
            }

            tcp.OnRead(OnAccept, OnError);
            for (int i = 0; i < NumberOfRequests; i++)
            {
                tcp.QueueWriteStream(this.content, this.OnWriteComplete);
            }

            this.batchWriteCommit = this.loop.NowInHighResolution;
            tcp.Shutdown(this.OnShutdown);
        }

        void OnShutdown(StreamHandle stream, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Tcp write batch : shutdown error {error}.");
            }

            this.batchWriteFinish = this.loop.NowInHighResolution;
            stream.CloseHandle(OnClosed);
            this.server.Shutdown();
        }

        static void OnAccept(StreamHandle stream, ReadableBuffer readableBuffer)
        {
            // NOP
        }

        static void OnError(StreamHandle stream, Exception exception) => 
            Console.WriteLine($"Tcp write batch : read error {exception}.");

        void OnWriteComplete(StreamHandle stream, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"Tcp write batch : write error {error}.");
            }

            this.writeCount++;
        }

        static void OnClosed(ScheduleHandle handle) => handle.Dispose();

        public void Dispose()
        {
            this.server = null;
            this.loop = null;
        }
    }
}
