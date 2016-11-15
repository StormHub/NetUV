// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;

    sealed class Pump : IDisposable
    {
        const int WriteBufferSize = 8192;
        const int StatisticsCount = 5;
        const int StatisticsInterval = 1000; // milliseconds
        const int MaximumWriteHandles = 1000; // backlog size

        readonly bool showIntervalStatistics;
        readonly HandleType handleType;
        readonly int clientCount;
        readonly List<IStream> clients;

        int connectedClientCount;
        int counnectionFailed;
        WritableBuffer dataBuffer;

        Loop loop;
        IDisposable server;
        Timer timer;

        long bytesRead;
        long totalBytesRead;

        long bytesWrite;
        long totalBytesWrite;

        int statisticsCount;

        long startTime;
        long stopTime;

        public Pump(HandleType handleType, int clientCount, bool showIntervalStatistics = false)
        {
            this.handleType = handleType;
            this.clientCount = clientCount;
            this.clients = new List<IStream>();
            this.showIntervalStatistics = showIntervalStatistics;
            this.counnectionFailed = 0;
        }

        static double ToGigaBytes(long total, long interval)
        {
            double bits = total * 8;

            bits /= 1024;
            bits /= 1024;
            bits /= 1024;

            double duration = interval / 1000d;
            return bits / duration;
        }

        public void Run()
        {
            this.StartServer();
            this.StartClient();

            this.loop.RunDefault();

            long diff = (this.stopTime - this.startTime);
            double total = ToGigaBytes(this.totalBytesWrite, diff);

            Console.WriteLine($"{this.handleType} pump {this.clientCount} client : {TestHelper.Format(total)} gbit/s");

            total = ToGigaBytes(this.totalBytesRead, diff);
            Console.WriteLine($"{this.handleType} pump {this.clientCount} server : {TestHelper.Format(total)} gbit/s");
        }

        void StartClient()
        {
            var data = new byte[WriteBufferSize];
            this.dataBuffer = WritableBuffer.From(data);

            for (int i = 0; i < this.clientCount; i++)
            {
                switch (this.handleType)
                {
                    case HandleType.Tcp:
                        this.loop
                            .CreateTcp()
                            .ConnectTo(TestHelper.LoopbackEndPoint, this.OnServerConnected);
                        break;
                    case HandleType.Pipe:
                        this.loop
                            .CreatePipe()
                            .ConnectTo(TestHelper.LocalPipeName, this.OnServerConnected);
                        break;
                    default:
                        throw new InvalidOperationException($"{this.handleType} is not supported.");
                }
            }
        }

        void OnServerConnected<T>(T client, Exception error) 
            where T : StreamHandle
        {
            if (error != null)
            {
                if (this.counnectionFailed == 0)
                {
                    Console.WriteLine($"{this.handleType} pump {this.clientCount} failed, {error.Message}");
                }
                client.Dispose();
                this.counnectionFailed++;
            }
            else
            {
                this.connectedClientCount++;

                IStream stream = client.CreateStream();
                this.clients.Add(stream);
                stream.Subscribe(ClientOnNext, this.ClientOnError, ClientOnComplete);
            }

            // Wait until all connected
            if (this.connectedClientCount != (this.clientCount - this.counnectionFailed))
            {
                return;
            }

            this.StartStatistics();
            foreach (IStream streamClient in this.clients)
            {
                streamClient.Write(this.dataBuffer, this.OnWriteComplete);
            }
        }

        void OnWriteComplete(IStream stream, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{this.handleType} pump {this.clientCount} client write error, {error}");
                stream.Dispose();
                return;
            }

            this.totalBytesWrite += WriteBufferSize;
            this.bytesWrite += WriteBufferSize;

            if (this.statisticsCount > 0)
            {
                stream.Write(this.dataBuffer, this.OnWriteComplete);
            }
            else
            {
                stream.Shutdown(this.OnShutdown);
            }
        }

        void OnShutdown(IStream stream, Exception exception)
        {
            if (this.clients.Contains(stream))
            {
                this.clients.Remove(stream);
            }

            stream.Dispose();

            if (this.clients.Count == 0)
            {
                this.server.Dispose();
            }
        }

        static void ClientOnNext(IStream stream, ReadableBuffer readableBuffer)
        {
            // NOP
        }

        void ClientOnError(IStream stream, Exception error)
        {
            Console.WriteLine($"{this.handleType} pump {this.clientCount} client read error, {error}");
            stream.Dispose();
        }

        static void ClientOnComplete(IStream stream) => stream.Dispose();

        void StartStatistics()
        {
            this.statisticsCount = StatisticsCount;
            this.timer = this.loop.CreateTimer();
            this.timer.Start(this.ShowStatistics, StatisticsInterval, StatisticsInterval);

            this.loop.UpdateTime();
            this.startTime = this.loop.Now;
        }

        void ShowStatistics(Timer handle)
        {
            if (this.showIntervalStatistics)
            {
                Console.WriteLine($"{this.handleType} pump connections : {this.connectedClientCount}, write : {ToGigaBytes(this.bytesWrite, StatisticsInterval)} gbit/s, read : {ToGigaBytes(this.bytesRead, StatisticsInterval)} gbit/s.");
            }

            this.statisticsCount--;

            if (this.statisticsCount > 0)
            {
                this.bytesWrite = 0;
                this.bytesRead = 0;
                return;
            }

            this.loop.UpdateTime();
            this.stopTime = this.loop.Now;

            handle.Stop();
            handle.Dispose();
        }

        void StartServer()
        {
            this.loop = new Loop();

            switch (this.handleType)
            {
                case HandleType.Tcp:
                    this.server = this.loop
                        .CreateTcp()
                        .SimultaneousAccepts(true)
                        .Listen(TestHelper.LoopbackEndPoint, this.OnClientConected, MaximumWriteHandles);
                    break;
                case HandleType.Pipe:
                    this.server = this.loop
                        .CreatePipe()
                        .Listen(TestHelper.LocalPipeName, this.OnClientConected, MaximumWriteHandles);
                    break;
                default:
                    throw new InvalidOperationException($"{this.handleType} is not supported.");
            }
        }

        void OnClientConected<T>(T client, Exception error) 
            where T : StreamHandle
        {
            if (error != null)
            {
                Console.WriteLine($"{this.handleType} pump {this.clientCount} client connection error, {error}");
                client.Dispose();
                return;
            }

            client.CreateStream()
                .Subscribe(this.ServerOnNext, this.ServerOnError, ServerOnComplete);
        }

        void ServerOnNext(IStream stream, ReadableBuffer buffer)
        {
            if (this.totalBytesRead == 0)
            {
                this.loop.UpdateTime();
                this.startTime = this.loop.Now;
            }

            int count = buffer.Count;
            this.bytesRead += count;
            this.totalBytesRead += count;
        }

        void ServerOnError(IStream stream, Exception error)
        {
            Console.WriteLine($"{this.handleType} pump {this.clientCount} server read error, {error}");
            stream.Dispose();
        }

        static void ServerOnComplete(IStream stream) => stream.Dispose();

        public void Dispose()
        {
            this.clients.Clear();
            this.dataBuffer.Dispose();
            this.server.Dispose();
            this.loop.Dispose();

            this.server = null;
            this.loop = null;
        }
    }
}
