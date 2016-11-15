// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;

    sealed class Pound : IDisposable
    {
        const long NanoSeconds = 1000000000;
        const string Message = "QS";

        readonly HandleType handleType;
        readonly int clientCount;
        readonly List<StreamHandle> clients;

        EchoServer server;
        Loop loop;
        WritableBuffer buffer;

        long start;

        long startTime;
        long stopTime;
        int closedStreams;
        int connectionsFailed;

        public Pound(HandleType handleType, int clientCount)
        {
            this.handleType = handleType;
            this.clientCount = clientCount;
            this.clients = new List<StreamHandle>();
        }

        public void Run()
        {
            this.closedStreams = 0;
            this.connectionsFailed = 0;

            this.server = new EchoServer(this.handleType);
            this.loop = this.server.Loop;

            this.buffer = WritableBuffer.From(Encoding.UTF8.GetBytes(Message));

            this.loop.UpdateTime();
            this.start = this.loop.Now;

            this.startTime = this.loop.NowInHighResolution;
            this.StartClients();

            this.loop.RunDefault();

            double duration = (this.stopTime - this.startTime) / (double)NanoSeconds;
            double value = this.closedStreams / duration;
            Console.WriteLine($"{this.handleType} conn pound : {this.clientCount} {TestHelper.Format(value)} accepts/s ({TestHelper.Format(this.connectionsFailed)} failed)");
        }

        void StartClients()
        {
            for (int i = 0; i < this.clientCount; i++)
            {
                StreamHandle streamHandle = this.CreateStream();
                this.clients.Add(streamHandle);
            }
        }

        StreamHandle CreateStream()
        {
            switch (this.handleType)
            {
                case HandleType.Tcp:
                    return this.loop
                        .CreateTcp()
                        .ConnectTo(TestHelper.LoopbackEndPoint, this.OnConnected);
                case HandleType.Pipe:
                    return this.loop
                        .CreatePipe()
                        .ConnectTo(TestHelper.LocalPipeName, this.OnConnected);
                default:
                    throw new InvalidOperationException($"{this.handleType} is not supported.");
            }
        }

        void OnConnected<T>(T client, Exception error) 
            where T : StreamHandle
        {
            if (error != null)
            {
                this.connectionsFailed++;
                client.CloseHandle(this.OnClosed);
            }
            else
            {
                IStream stream = client.CreateStream();
                stream.Subscribe(OnNext, this.OnError);
                stream.Write(this.buffer, this.OnWriteComplete);
            }
        }

        void OnWriteComplete(IStream stream, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{this.handleType} conn pound : {this.clientCount} write error {error}");
            }

            stream.Handle.CloseHandle(this.OnClosed);
        }

        void OnClosed(StreamHandle stream)
        {
            this.closedStreams++;
            stream.Dispose();

            if ((this.loop.Now - this.start) < 10000)
            {
                int index = this.clients.IndexOf(stream);
                this.clients[index] = this.CreateStream();
                return;
            }

            if (this.stopTime == 0)
            {
                this.stopTime = this.loop.NowInHighResolution;
            }
            this.clients.Remove(stream);

            if (this.clients.Count == 0)
            {
                this.loop.Stop();
            }
        }

        static void OnNext(IStream stream, ReadableBuffer data)
        {
            // NOP
        }

        void OnError(IStream stream, Exception error)
        {
            if (error == null)
            {
                return;
            }

            Console.WriteLine($"{this.handleType} conn pound : {this.clientCount} read error {error}");
        }

        public void Dispose()
        {
            this.server.Dispose();
            this.server = null;
            this.loop = null;
        }
    }
}
