// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Net;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;
    using Xunit;

    public sealed class UdpBindTests : IDisposable
    {
        const int Port = 8989;

        Loop loop;
        int closeCount;

        public UdpBindTests()
        {
            this.loop = new Loop();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Bind(bool reuseAddress)
        {
            var anyEndPoint = new IPEndPoint(IPAddress.Any, Port);

            Udp udp1 = this.loop.CreateUdp();
            udp1.Bind(anyEndPoint, reuseAddress);

            Udp udp2 = this.loop.CreateUdp();
            if (reuseAddress)
            {
                udp2.Bind(anyEndPoint, true);
            }
            else
            {
                var error = Assert.Throws<OperationException>(() => udp2.Bind(anyEndPoint));
                Assert.Equal((int)uv_err_code.UV_EADDRINUSE, error.ErrorCode);
            }

            udp1.CloseHandle(this.OnClose);
            udp2.CloseHandle(this.OnClose);

            this.loop.RunDefault();
            Assert.Equal(2, this.closeCount);
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
