// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Net;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;
    using Xunit;

    public sealed class UdpSendAndReceiveTests : IDisposable
    {
        const int Port = 8997;
        Loop loop;

        int closeCount;
        int clientReceiveCount;
        int clientSendCount;
        int serverReceiveCount;
        int serverSendCount;

        [Fact]
        public void Run()
        {
            this.closeCount = 0;
            this.clientReceiveCount = 0;
            this.clientSendCount = 0;
            this.serverReceiveCount = 0;
            this.serverSendCount = 0;

            this.loop = new Loop();

            var anyEndPoint = new IPEndPoint(IPAddress.Any, Port);
            this.loop
                .CreateUdp()
                .ReceiveStart(anyEndPoint, this.OnServerReceive);

            Udp client = this.loop.CreateUdp();

            byte[] data = Encoding.UTF8.GetBytes("PING");
            var remoteEndPoint = new IPEndPoint(IPAddress.Loopback, Port);
            client.QueueSend(data, remoteEndPoint, this.OnClientSendCompleted);

            this.loop.RunDefault();

            Assert.Equal(1, this.clientSendCount);
            Assert.Equal(1, this.serverSendCount);
            Assert.Equal(1, this.serverReceiveCount);
            Assert.Equal(1, this.clientReceiveCount);
            Assert.Equal(2, this.closeCount);
        }

        void OnClientReceive(Udp udp, IDatagramReadCompletion completion)
        {
            if (completion.Error != null
                || completion.RemoteEndPoint == null)
            {
                return;
            }

            ReadableBuffer buffer = completion.Data;
            if (buffer.Count == 0)
            {
                return;
            }

            string message = buffer.ReadString(buffer.Count, Encoding.UTF8);
            if (message == "PONG")
            {
                this.clientReceiveCount++;
            }

            udp.CloseHandle(this.OnClose);
        }

        void OnClientSendCompleted(Udp udp, Exception exception)
        {
            if (exception != null)
            {
                return;
            }

            udp.ReceiveStart(this.OnClientReceive);
            this.clientSendCount++;
        }

        void OnServerReceive(Udp udp, IDatagramReadCompletion completion)
        {
            if (completion.Error != null 
                || completion.RemoteEndPoint == null)
            {
                return;
            }

            ReadableBuffer buffer = completion.Data;
            if (buffer.Count == 0)
            {
                return;
            }

            string message = buffer.ReadString(buffer.Count, Encoding.UTF8);
            if (message == "PING")
            {
                this.serverReceiveCount++;
            }

            udp.ReceiveStop();

            byte[] data = Encoding.UTF8.GetBytes("PONG");
            udp.QueueSend(data, completion.RemoteEndPoint, this.OnServerSendCompleted);
        }

        void OnServerSendCompleted(Udp udp, Exception exception)
        {
            if (exception == null)
            {
                this.serverSendCount++;
            }

            udp.CloseHandle(this.OnClose);
        }

        void OnClose(Udp handle)
        {
            handle.Dispose();
            this.closeCount++;
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
