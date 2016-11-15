// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using System.Threading.Tasks;
    using NetUV.Core.Handles;
    using Xunit;

    public sealed class AsyncTests : IDisposable
    {
        Loop loop;
        Prepare prepare;
        Async async;
        Task task;

        int prepareCalled;
        int asyncCalled;

        void AsyncSend()
        {
            while (this.asyncCalled < 3)
            {
                this.async.Send();
            }
        }

        void PrepareCallback(Prepare handle)
        {
            if (handle != null
                && this.prepareCalled == 0)
            {
                this.prepareCalled++;

                this.task = Task.Run(() => this.AsyncSend());
            }
        }

        void OnAsync(Async handle)
        {
            if (handle != null)
            {
                int n = ++this.asyncCalled;

                if (n == 3)
                {
                    this.prepare.Dispose();
                    this.async.Dispose();
                }
            }
        }

        [Fact]
        public void Async()
        {
            this.loop = new Loop();
            this.prepareCalled = 0;
            this.asyncCalled = 0;

            this.prepare = this.loop.CreatePrepare();
            this.prepare.Start(this.PrepareCallback);

            this.async = this.loop.CreateAsync(this.OnAsync);

            this.loop.RunDefault();

            Assert.True(this.task.IsCompleted);
            Assert.True(this.prepareCalled > 0, "Prepare callback should be called at least once.");
            Assert.Equal(3, this.asyncCalled);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
