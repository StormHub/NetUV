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

    public sealed class UdpSendUnreachableTests : IDisposable
    {
        const int Port1 = 8995;
        const int Port2 = 8994;

        Loop loop;
        Udp client;

        int timerCount;
        int closeCount;
        int clientSendCount;
        int clientReceiveCount;

        [Fact]
        public void Run()
        {
            this.timerCount = 0;
            this.closeCount = 0;
            this.clientSendCount = 0;
            this.clientReceiveCount = 0;

            var endPoint1 = new IPEndPoint(IPAddress.Loopback, Port1);
            var endPoint2 = new IPEndPoint(IPAddress.Loopback, Port2);

            this.loop = new Loop();
            this.loop
                .CreateTimer()
                .Start(this.OnTimer, 1000, 0);

            this.client = this.loop
                .CreateUdp()
                .Bind(endPoint2);

            // Client read should not get any results
            this.client.ReceiveStart(this.OnReceive);

            byte[] data1 = Encoding.UTF8.GetBytes("PING");
            byte[] data2 = Encoding.UTF8.GetBytes("PANG");
            this.client.QueueSend(data1, endPoint1, this.OnSendCompleted);
            this.client.QueueSend(data2, endPoint1, this.OnSendCompleted);

            this.loop.RunDefault();

            Assert.Equal(1, this.timerCount);
            Assert.Equal(2, this.clientSendCount);
            Assert.Equal(2, this.clientReceiveCount);
            Assert.Equal(2, this.closeCount);
        }

        void OnReceive(Udp udp, IDatagramReadCompletion completion)
        {
            if (completion.Error != null)
            {
                return;
            }

            ReadableBuffer data = completion.Data;
            if (data.Count == 0 
                && completion.RemoteEndPoint == null)
            {
                this.clientReceiveCount++;
            }
        }

        void OnSendCompleted(Udp udp, Exception exception)
        {
            if (exception == null)
            {
                this.clientSendCount++;
            }
        }

        void OnTimer(Timer handle)
        {
            this.timerCount++;
            this.client?.CloseHandle(this.OnClose);
            handle.CloseHandle(this.OnClose);
        }

        void OnClose(ScheduleHandle handle)
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
