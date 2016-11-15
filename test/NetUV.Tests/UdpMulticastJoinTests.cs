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

    public sealed class UdpMulticastJoinTests : IDisposable
    {
        const int Port = 8898;

        Loop loop;
        int closeCount;
        int serverSendCount;
        int clientReceiveCount;

        [Fact]
        public void Run()
        {
            this.closeCount = 0;
            this.serverSendCount = 0;
            this.clientReceiveCount = 0;

            this.loop = new Loop();

            var endPoint = new IPEndPoint(IPAddress.Loopback, Port);
            Udp client = this.loop
                .CreateUdp()
                .ReceiveStart(endPoint, this.OnClientReceive);

            IPAddress group = IPAddress.Parse("239.255.0.1");
            client.JoinGroup(group);

            byte[] data = Encoding.UTF8.GetBytes("PING");
            Udp server = this.loop.CreateUdp();
            server.QueueSend(data, endPoint, this.OnServerSendCompleted);

            this.loop.RunDefault();

            Assert.Equal(2, this.closeCount);
            Assert.Equal(1, this.serverSendCount);
            Assert.Equal(1, this.clientReceiveCount);
        }

        void OnServerSendCompleted(Udp udp, Exception exception)
        {
            if (exception == null)
            {
                this.serverSendCount++;
            }

            udp.CloseHandle(this.OnClose);
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
            if (message == "PING")
            {
                this.clientReceiveCount++;
            }

            /* we are done with the client handle, we can close it */
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
