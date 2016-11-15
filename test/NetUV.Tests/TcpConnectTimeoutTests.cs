// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Net;
    using NetUV.Core.Handles;
    using NetUV.Core.Native;
    using Xunit;

    public sealed class TcpConnectTimeoutTests : IDisposable
    {
        Loop loop;
        Tcp tcp;

        int connectCount;
        int closeCount;

        public TcpConnectTimeoutTests()
        {
            this.loop = new Loop();
            this.connectCount = 0;
            this.closeCount = 0;
        }

        /* Verify that connecting to an unreachable address or port doesn't hang
         * the event loop.
         */
        [Fact]
        public void Run()
        {
            IPAddress ipAddress = IPAddress.Parse("8.8.8.8");
            var endPoint = new IPEndPoint(ipAddress, 9999);

            this.loop
                .CreateTimer()
                .Start(this.OnTimer, 50, 0);

            this.tcp = this.loop.CreateTcp();

            try
            {
                this.tcp = this.loop
                    .CreateTcp()
                    .ConnectTo(endPoint, this.OnConnected);
            }
            catch (OperationException exception)
            {
                // Skip
                if (exception.ErrorCode == (int)uv_err_code.UV_ENETUNREACH)
                {
                    return;
                }
            }

            this.loop.RunDefault();
            Assert.Equal(2, this.closeCount);
            Assert.Equal(1, this.connectCount);
        }

        void OnConnected(Tcp tcpClient, Exception exception)
        {
            Assert.IsType<OperationException>(exception);

            var operationException = (OperationException)exception;
            Assert.Equal((int)uv_err_code.UV_ECANCELED, operationException.ErrorCode);
            this.connectCount++;
        }

        void OnTimer(Timer timer)
        {
            this.tcp.CloseHandle(this.OnClose);
            timer.CloseHandle(this.OnClose);
        }

        void OnClose(Timer timer)
        {
            timer.Dispose();
            this.closeCount++;
        }

        void OnClose(Tcp tcpClient)
        {
            tcpClient.Dispose();
            this.closeCount++;
        }

        public void Dispose()
        {
            this.tcp.Dispose();
            this.tcp = null;

            this.loop.Dispose();
            this.loop = null;
        }
    }
}
