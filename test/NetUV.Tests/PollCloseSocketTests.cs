// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using NetUV.Core.Handles;
    using Xunit;

    public sealed class PollCloseSocketTests : IDisposable
    {
        const int Port = 9989;
        Loop loop;
        Socket socket;
        int closeCount;

        [Fact]
        public void Run()
        {
            this.loop = new Loop();

            var endPoint = new IPEndPoint(IPAddress.Loopback, Port);
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // There should be nothing listening on this
            this.socket.ConnectAsync(endPoint);

            IntPtr handle = TestHelper.GetHandle(this.socket);

            this.loop
                .CreatePoll(handle)
                .Start(PollMask.Writable, this.OnPoll);

            this.loop.RunDefault();
            Assert.Equal(1, this.closeCount);
        }

        void OnPoll(Poll poll, PollStatus status)
        {
            poll.Start(PollMask.Readable, this.OnPoll);
            this.socket?.Dispose();
            poll.CloseHandle(this.OnClose);
        }

        void OnClose(Poll handle)
        {
            handle.Dispose();
            this.closeCount++;
        }

        public void Dispose()
        {
            this.socket?.Dispose();

            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
