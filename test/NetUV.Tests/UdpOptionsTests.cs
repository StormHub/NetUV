// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;
    using Xunit;

    public sealed class UdpOptionsTests : IDisposable
    {
        const int Port = 8999;

        Loop loop;

        public UdpOptionsTests()
        {
            this.loop = new Loop();
        }


        static IEnumerable<object[]> IpFamilyCases()
        {
            yield return new object[] { new IPEndPoint(IPAddress.Any, Port) };
            yield return new object[] { new IPEndPoint(IPAddress.IPv6Any, Port) };
        }

        [Theory]
        [MemberData(nameof(IpFamilyCases))]
        public void IpFamily(IPEndPoint endPoint)
        {
            Udp udp = this.loop.CreateUdp();

            /* don't keep the loop alive */
            udp.RemoveReference();
            udp.Bind(endPoint);

            udp.Broadcast(true);
            udp.Broadcast(true);
            udp.Broadcast(false);
            udp.Broadcast(false);

            /* values 1-255 should work */
            for (int i = 1; i <= 255; i++)
            {
                udp.Ttl(i);
            }

            var invalidTtls = new [] { -1, 0, 256 };
            foreach (int i in invalidTtls)
            {
                var error = Assert.Throws<OperationException>(() => udp.Ttl(i));
                Assert.Equal((int)uv_err_code.UV_EINVAL, error.ErrorCode);
            }

            udp.MulticastLoopback(true);
            udp.MulticastLoopback(true);
            udp.MulticastLoopback(false);
            udp.MulticastLoopback(false);

            /* values 0-255 should work */
            for (int i = 0; i <= 255; i++)
            {
                udp.MulticastTtl(i);
            }

            /* anything >255 should fail */
            var exception = Assert.Throws<OperationException>(() => udp.MulticastTtl(256));
            Assert.Equal((int)uv_err_code.UV_EINVAL, exception.ErrorCode);
            /* don't test ttl=-1, it's a valid value on some platforms */

            this.loop.RunDefault();
        }

        [Fact]
        public void NoBind()
        {
            Udp udp = this.loop.CreateUdp();

            var error = Assert.Throws<OperationException>(() => udp.MulticastTtl(32));
            Assert.Equal((int)uv_err_code.UV_EBADF, error.ErrorCode);

            error = Assert.Throws<OperationException>(() => udp.Broadcast(true));
            Assert.Equal((int)uv_err_code.UV_EBADF, error.ErrorCode);

            error = Assert.Throws<OperationException>(() => udp.Ttl(1));
            Assert.Equal((int)uv_err_code.UV_EBADF, error.ErrorCode);

            error = Assert.Throws<OperationException>(() => udp.MulticastInterface(IPAddress.Any));
            Assert.Equal((int)uv_err_code.UV_EBADF, error.ErrorCode);

            udp.CloseHandle(OnClose);

            this.loop.RunDefault();
        }

        static void OnClose(Udp handle) => handle.Dispose();

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
