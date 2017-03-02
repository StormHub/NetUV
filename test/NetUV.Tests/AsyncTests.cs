// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Threading;
    using NetUV.Core.Handles;
    using Xunit;

    public sealed class AsyncTests : IDisposable
    {
        Loop loop;
        Prepare prepare;
        Async async;

        Thread thread;
        int prepareCalled;
        long asyncCalled;
        int closeCount;

        void PrepareCallback(Prepare handle)
        {
            if (this.prepareCalled == 0)
            {
                this.thread = new Thread(this.ThreadStart);
                this.thread.Start();
            }

            this.prepareCalled++;
        }

        void ThreadStart()
        {
            while (true)
            {
                if (Interlocked.Read(ref this.asyncCalled) == 3)
                {
                    break;
                }

                this.async.Send();
            }
        }

        void OnAsync(Async handle)
        {
            if (Interlocked.Increment(ref this.asyncCalled) == 3)
            {
                this.prepare.CloseHandle(this.OnClose);
                this.async.CloseHandle(this.OnClose);
            }
        }

        void OnClose(ScheduleHandle handle)
        {
            handle.Dispose();
            this.closeCount++;
        }

        [Fact]
        public void Run()
        {
            this.loop = new Loop();
            this.prepareCalled = 0;
            this.asyncCalled = 0;

            this.prepare = this.loop
                .CreatePrepare()
                .Start(this.PrepareCallback);
            this.async = this.loop.CreateAsync(this.OnAsync);

            this.loop.RunDefault();

            this.thread?.Join();

            Assert.Equal(2, this.closeCount);
            Assert.True(this.prepareCalled > 0);
            Assert.Equal(3, this.asyncCalled);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
