// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Net;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;
    using Xunit;

    public sealed class TcpBindErrorTests : IDisposable
    {
        const int Port = 9887;
        Loop loop;
        int closeCalled;

        public TcpBindErrorTests()
        {
            this.loop = new Loop();
            this.closeCalled = 0;
        }

        [Fact]
        public void AddressInUse()
        {
            IPAddress address = IPAddress.Loopback;
            var endPoint = new IPEndPoint(address, Port);

            Tcp tcp1 = this.loop.CreateTcp().Bind(endPoint);
            Tcp tcp2 = this.loop.CreateTcp().Bind(endPoint);

            StreamListener<Tcp> listener = tcp1.Listen(OnConnection);
            Assert.Throws<OperationException>(() => tcp2.Listen(OnConnection));

            tcp1.CloseHandle(this.OnClose);
            tcp2.CloseHandle(this.OnClose);
            this.loop.RunDefault();
            Assert.Equal(2, this.closeCalled);
            listener.Dispose();
        }

        [Theory]
        [InlineData("4.4.4.4")]
        [InlineData("127.255.255.255")]
        public void AddressNotAvailable(string ipAddress)
        {
            IPAddress address = IPAddress.Parse(ipAddress);
            var endPoint = new IPEndPoint(address, Port);
            Tcp tcp = this.loop.CreateTcp();
            Assert.Throws<OperationException>(() => tcp.Bind(endPoint));

            tcp.CloseHandle(this.OnClose);
            this.loop.RunDefault();
            Assert.Equal(1, this.closeCalled);
        }

        [Fact]
        public void Invalid()
        {
            IPAddress address = IPAddress.Loopback;
            var endPoint1 = new IPEndPoint(address, Port);
            var endPoint2 = new IPEndPoint(address, Port + 1);

            Tcp tcp = this.loop.CreateTcp();
            Assert.Equal(tcp.Bind(endPoint1), tcp);
            
            Assert.Throws<OperationException>(() => tcp.Bind(endPoint2));
            tcp.CloseHandle(this.OnClose);
            this.loop.RunDefault();
            Assert.Equal(1, this.closeCalled);
        }

        [Fact]
        public void LocalHost()
        {
            IPAddress address = IPAddress.Loopback;
            var endPoint = new IPEndPoint(address, Port);

            Tcp tcp = this.loop.CreateTcp();
            tcp.Bind(endPoint);
            tcp.Dispose();
        }

        [Fact]
        public void InvalidFlag()
        {
            IPAddress address = IPAddress.Loopback;
            var endPoint = new IPEndPoint(address, Port);

            Tcp tcp = this.loop.CreateTcp();
            Assert.Throws<OperationException>(() => tcp.Bind(endPoint, true));
            tcp.Dispose();
        }

        [Fact]
        public void ListenWithoutBind()
        {
            Tcp tcp = this.loop.CreateTcp();
            StreamListener<Tcp> listener = tcp.Listen(OnConnection);
            listener.Dispose();
        }

        static void OnConnection(Tcp tcp, Exception exception) =>
            Assert.True(exception == null);

        void OnClose(Tcp tcp)
        {
            tcp.Dispose();
            this.closeCalled++;
        }

        public void Dispose()
        {
            this.loop.Dispose();
            this.loop = null;
        }
    }
}
