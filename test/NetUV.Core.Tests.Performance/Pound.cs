// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;

    sealed class Pound : IDisposable
    {
        const long NanoSeconds = 1000000000;
        const string Message = "QS";

        readonly HandleType handleType;
        readonly int clientCount;
        readonly byte[] content;

        EchoServer server;
        Loop loop;

        long start;

        long startTime;
        long stopTime;
        int closedStreams;
        int connectionsFailed;
        int activeStreams;

        class StreamContext
        {
            public StreamContext(int index, StreamHandle[] streams)
            {
                this.Index = index;
                this.Streams = streams;
            }

            public int Index { get; }

            public StreamHandle[] Streams { get; }
        }

        public Pound(HandleType handleType, int clientCount)
        {
            this.handleType = handleType;
            this.clientCount = clientCount;
            this.content = Encoding.UTF8.GetBytes(Message);
        }

        public void Run()
        {
            this.closedStreams = 0;
            this.connectionsFailed = 0;

            this.server = new EchoServer(this.handleType);
            this.loop = this.server.Loop;

            this.loop.UpdateTime();
            this.start = this.loop.Now;

            this.startTime = this.loop.NowInHighResolution;
            this.StartClients();
            this.activeStreams = this.clientCount;

            this.loop.RunDefault();

            double duration = (this.stopTime - this.startTime) / (double)NanoSeconds;
            double value = this.closedStreams / duration;
            Console.WriteLine($"{this.handleType} conn pound : {this.clientCount} {TestHelper.Format(value)} accepts/s ({TestHelper.Format(this.connectionsFailed)} failed)");
        }

        void StartClients()
        {
            var streams = new StreamHandle[this.clientCount];
            for (int i = 0; i < this.clientCount; i++)
            {
                streams[i] = this.CreateStream();
                streams[i].UserToken = new StreamContext(i, streams);
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
                client.OnRead(this.OnAccept, this.OnError, this.OnCompleted);
                client.QueueWriteStream(this.content, this.OnWriteComplete);
            }
        }

        void OnWriteComplete(StreamHandle stream, Exception error)
        {
            if (error != null)
            {
                stream.CloseHandle(this.OnClosed);
                Console.WriteLine($"{this.handleType} conn pound : {this.clientCount} write error {error}");
            }
        }

        void OnClosed(StreamHandle stream)
        {
            var context = (StreamContext)stream.UserToken;
            this.closedStreams++;
            stream.Dispose();

            long duration = this.loop.Now - this.start;
            if (duration < 10000)
            {
                StreamHandle streamHandle = this.CreateStream();
                streamHandle.UserToken = context;
                context.Streams[context.Index] = streamHandle;
            }
            else
            {
                context.Streams[context.Index] = null;
                this.activeStreams--;

                if (this.stopTime == 0)
                {
                    this.stopTime = this.loop.NowInHighResolution;
                }

                if (this.activeStreams <= 0) 
                {
                    this.server.CloseServer();
                }
            }
        }

        void OnAccept(StreamHandle stream, ReadableBuffer data) => stream.CloseHandle(this.OnClosed);

        void OnError(StreamHandle stream, Exception error)
        {
            var exception = error as OperationException;
            if (exception != null 
                && (exception.ErrorCode == ErrorCode.ECONNRESET 
                || exception.ErrorCode == ErrorCode.ETIMEDOUT))
            {
                this.connectionsFailed++;
            }
            else
            {
                Console.WriteLine($"{this.handleType} conn pound : {this.clientCount} read error {error}");
            }

            stream.CloseHandle(this.OnClosed);
        }

        void OnCompleted(StreamHandle stream) => stream.CloseHandle(this.OnClosed);

        public void Dispose()
        {
            this.server.Dispose();
            this.server = null;
            this.loop = null;
        }
    }
}
