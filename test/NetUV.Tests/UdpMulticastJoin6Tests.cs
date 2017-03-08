// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Net;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;
    using Xunit;

    public sealed class UdpMulticastJoin6Tests : IDisposable
    {
        const int Port = 9889;

        Loop loop;

        int closeCount;
        int serverSendCount;
        int clientReceiveCount;

        [Fact]
        public void Run()
        {
            if (!Platform.OSSupportsIPv6)
            {
                return;
            }

            this.closeCount = 0;
            this.serverSendCount = 0;
            this.clientReceiveCount = 0;

            this.loop = new Loop();
            
            var endPoint = new IPEndPoint(IPAddress.IPv6Loopback, Port);
            Udp client = this.loop.CreateUdp();

            try
            {
                client.ReceiveStart(endPoint, this.OnClientReceive);
            }
            catch (OperationException exception)
            {
                // IPv6 loop back not available happens on some Linux
                Assert.Equal(ErrorCode.EADDRNOTAVAIL, exception.ErrorCode);
                return;
            }

            IPAddress group = IPAddress.Parse("ff02::1");
            try
            {
                if (Platform.IsMacOS)
                {
                    client.JoinGroup(group, IPAddress.IPv6Loopback);
                }
                else
                {
                    client.JoinGroup(group);
                }
            }
            catch (OperationException error)
            {
                Assert.Equal(ErrorCode.ENODEV, error.ErrorCode);
                return;
            }

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
