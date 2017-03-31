// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    sealed class Pump : IDisposable
    {
        const int WriteBufferSize = 8192;
        const int MaxSimultaneousConnects = 100;
        const int StatisticsCount = 5;
        const int StatisticsInterval = 1000; // milliseconds
        const int MaximumWriteHandles = 1000; // backlog size

        readonly bool showIntervalStatistics;
        readonly HandleType handleType;
        readonly int targetConnections;

        Loop loop;
        StreamHandle server;
        WritableBuffer dataBuffer;

        StreamHandle[] writeHandles;

        int maxConnectSocket;
        int writeSockets;
        int readSockets;
        int maxReadSockets;
        int statsLeft;

        long startTime;
        long stopTime;

        long receive;
        long receiveTotal;
        long sent;
        long sentTotal;

        int connectionErrorCount;

        public Pump(HandleType handleType, int clientCount, bool showIntervalStatistics = false)
        {
            this.handleType = handleType;
            this.targetConnections = clientCount;
            this.showIntervalStatistics = showIntervalStatistics;

            var data = new byte[WriteBufferSize];
            this.dataBuffer = WritableBuffer.From(data);

            this.writeHandles = new StreamHandle[MaximumWriteHandles];
        }

        public void Run()
        {
            this.loop = new Loop();

            // Start server
            switch (this.handleType)
            {
                case HandleType.Tcp:
                    this.server = this.loop
                        .CreateTcp()
                        .SimultaneousAccepts(true)
                        .Listen(TestHelper.LoopbackEndPoint, this.OnConnection, MaximumWriteHandles);
                    break;
                case HandleType.Pipe:
                    this.server = this.loop
                        .CreatePipe()
                        .Listen(TestHelper.LocalPipeName, this.OnConnection, MaximumWriteHandles);
                    break;
                default:
                    throw new InvalidOperationException($"{this.handleType} is not supported.");
            }

            this.ConnectToServer();

            this.loop.RunDefault();

            long diff = (this.stopTime - this.startTime);
            double total = ToGigaBytes(this.sentTotal, diff);
            Console.WriteLine($"{this.handleType} pump {this.targetConnections} client : {TestHelper.Format(total)} gbit/s");

            total = ToGigaBytes(this.receiveTotal, diff);
            Console.WriteLine($"{this.handleType} pump {this.targetConnections} server : {TestHelper.Format(total)} gbit/s ({this.readSockets})");
        }

        void ConnectToServer()
        {
            while (this.maxConnectSocket < this.targetConnections 
                && this.maxConnectSocket < this.writeSockets + MaxSimultaneousConnects)
            {
                switch (this.handleType)
                {
                    case HandleType.Tcp:
                        this.writeHandles[this.maxConnectSocket] = this.loop
                            .CreateTcp()
                            .ConnectTo(TestHelper.LoopbackEndPoint, this.OnConnected);
                        break;
                    case HandleType.Pipe:
                        this.writeHandles[this.maxConnectSocket] = this.loop
                            .CreatePipe()
                            .ConnectTo(TestHelper.LocalPipeName, this.OnConnected);
                        break;
                    default:
                        throw new InvalidOperationException($"{this.handleType} is not supported.");
                }

                this.maxConnectSocket++;
            }
        }

        void OnConnected<T>(T client, Exception error) where T : StreamHandle
        {
            if (error != null)
            {
                if (this.connectionErrorCount == 0)
                {
                    Console.WriteLine($"{this.handleType} pump {this.targetConnections} error : {error}.");
                    this.connectionErrorCount++;
                }

                client.CloseHandle(OnClosed);
            }

            this.writeSockets++;
            this.ConnectToServer();

            // Wait all connected to start writing
            if (this.writeSockets == this.targetConnections)
            {
                this.StartStatistics();

                for (int i = 0; i < this.writeSockets; i++)
                {
                    if (this.writeHandles[i].IsActive)
                    {
                        this.writeHandles[i].QueueWriteStream(this.dataBuffer, this.OnWriteCompleted);
                    }
                }
            }
        }

        void StartStatistics()
        {
            this.statsLeft = StatisticsCount;
            this.loop
                .CreateTimer()
                .Start(this.ShowStatistics, StatisticsInterval, StatisticsInterval);

            this.loop.UpdateTime();
            this.startTime = this.loop.Now;
        }

        void ShowStatistics(Timer handle)
        {
            if (this.showIntervalStatistics)
            {
                double sentStat = ToGigaBytes(this.sent, StatisticsInterval);
                double receiveStat = ToGigaBytes(this.receive, StatisticsInterval);
                Console.WriteLine($"{this.handleType} pump connections : {this.targetConnections},"
                    + $" write : {TestHelper.Format(sentStat)} gbit/s, "
                    + $" read : {TestHelper.Format(receiveStat)} gbit/s. {this.maxReadSockets} {this.readSockets}");
            }

            this.statsLeft--;
            if (this.statsLeft == 0)
            {
                this.loop.UpdateTime();
                this.stopTime = this.loop.Now;

                for (int i = 0; i < this.writeSockets; i++)
                {
                    this.writeHandles[i].CloseHandle(OnClosed);
                }

                handle.Stop();
                handle.CloseHandle(OnClosed);
                this.server.CloseHandle(OnClosed);
            }
            else
            {
                this.receive = 0;
                this.sent = 0;
            }
        }

        void OnWriteCompleted(StreamHandle handle, Exception error)
        {
            if (error != null)
            {
                handle.CloseHandle(OnClosed);
                return;
            }

            this.sent += WriteBufferSize;
            this.sentTotal += WriteBufferSize;

            if (this.statsLeft > 0 
                && handle.IsActive)
            {
                handle.QueueWriteStream(this.dataBuffer, this.OnWriteCompleted);
            }
        }

        void OnConnection<T>(T client, Exception error) where T : StreamHandle
        {
            if (error != null)
            {
                client.CloseHandle(OnClosed);
            }
            else
            {
                this.readSockets++;
                this.maxReadSockets++;
                client.OnRead(this.OnAccept, OnError);
            }
        }

        void OnAccept(StreamHandle stream, ReadableBuffer readableBuffer)
        {
            if (this.receiveTotal == 0)
            {
                this.loop.UpdateTime();
                this.startTime = this.loop.Now;
            }

            this.receive += readableBuffer.Count;
            this.receiveTotal += readableBuffer.Count;
        }

        static void OnError(StreamHandle stream, Exception error) => stream.CloseHandle(OnClosed);

        static void OnClosed(ScheduleHandle handle) => handle.Dispose();

        static double ToGigaBytes(long total, long interval)
        {
            double bits = total * 8;

            bits /= 1024;
            bits /= 1024;
            bits /= 1024;

            double duration = interval / 1000d;
            return bits / duration;
        }

        public void Dispose()
        {
            this.dataBuffer.Dispose();
            this.writeHandles = null;

            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
