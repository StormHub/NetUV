// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Net;
    using System.Text;
    using NetUV.Core.Handles;
    using Xunit;

    public sealed class UdpMulticastInterface6Tests : IDisposable
    {
        const int Port = 8993;

        Loop loop;

        int closeCount;
        int serverSendCount;

        [Fact]
        public void Run()
        {
            this.closeCount = 0;
            this.serverSendCount = 0;

            this.loop = new Loop();

            var anyEndPoint = new IPEndPoint(IPAddress.IPv6Any, Port);
            Udp server = this.loop.CreateUdp()
                .Bind(anyEndPoint)
                .MulticastInterface(IPAddress.IPv6Loopback);

            var endPoint = new IPEndPoint(IPAddress.IPv6Loopback, Port);
            byte[] data = Encoding.UTF8.GetBytes("PING");
            server.QueueSend(data, endPoint, this.OnServerSendCompleted);

            this.loop.RunDefault();

            Assert.Equal(1, this.closeCount);
            Assert.Equal(1, this.serverSendCount);
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
