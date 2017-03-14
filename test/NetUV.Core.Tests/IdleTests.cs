// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests
{
    using System;
    using NetUV.Core.Handles;
    using Xunit;

    public sealed class IdleTests : IDisposable
    {
        Loop loop;
        Idle idle;
        Check check;
        Timer timer;

        int idleCalled;
        int checkCalled;
        int timerCalled;

        void OnIdle(Idle handle)
        {
            if (handle != null)
            {
                this.idleCalled++;
            }
        }

        void OnCheck(Check handle)
        {
            if (handle != null 
                && handle == this.check)
            {
                this.checkCalled++;
            }
        }

        void OnTimer(Timer handle)
        {
            if (handle != null 
                && handle == this.timer)
            {
                this.idle?.Dispose();
                this.check?.Dispose();
                this.timer?.Dispose();

                this.timerCalled++;
            }
        }

        [Fact]
        public void IdleStarvation()
        {
            this.loop = new Loop();

            this.idle = this.loop.CreateIdle();
            this.idle.Start(this.OnIdle);

            this.check = this.loop.CreateCheck();
            this.check.Start(this.OnCheck);

            this.timer = this.loop.CreateTimer();
            this.timer.Start(this.OnTimer, 50, 0);

            this.loop.RunDefault();

            Assert.True(this.idleCalled > 0, "Idle callback should be invoked at least once.");
            Assert.Equal(1, this.timerCalled);
            Assert.True(this.checkCalled > 0, "Check callback should be invoked at least once.");

            Assert.NotNull(this.idle);
            Assert.False(this.idle.IsValid);

            Assert.NotNull(this.check);
            Assert.False(this.check.IsValid);

            Assert.NotNull(this.timer);
            Assert.False(this.timer.IsValid);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
