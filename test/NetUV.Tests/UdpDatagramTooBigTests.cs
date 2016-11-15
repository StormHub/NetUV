// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Net;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;
    using Xunit;

    public sealed class UdpDatagramTooBigTests : IDisposable
    {
        const int Port = 8989;

        Loop loop;
        int sendCount;
        int closeCount;

        [Fact]
        public void Run()
        {
            this.loop = new Loop();
            Udp udp = this.loop.CreateUdp();

            /* 64K MTU is unlikely, even on localhost */
            var data = new byte[65536]; 

            var endPoint = new IPEndPoint(IPAddress.Loopback, Port);
            udp.QueueSend(data, endPoint, this.OnSendCompleted);

            this.loop.RunDefault();

            Assert.Equal(1, this.sendCount);
            Assert.Equal(1, this.closeCount);
        }

        void OnSendCompleted(Udp udp, Exception exception)
        {
            var error = exception as OperationException;
            if (error != null 
                && error.ErrorCode == (int)uv_err_code.UV_EMSGSIZE)
            {
                this.sendCount++;
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
