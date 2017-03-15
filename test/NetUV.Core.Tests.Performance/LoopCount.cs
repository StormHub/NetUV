// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using NetUV.Core.Handles;

    sealed class LoopCount : IDisposable
    {
        const long NanoSeconds = 1000000000;
        const long NumberOfTicks = (2 * 1000 * 1000);

        Idle idle;
        long ticks;

        public void Run()
        {
            this.ticks = 0;
            this.RunCount();
            this.ticks = 0;
            this.RunTimed();
        }

        void RunCount()
        {
            var loop = new Loop();

            this.idle = loop.CreateIdle();
            this.idle.Start(this.OnIdleTickCallback);

            long start = loop.NowInHighResolution;
            loop.RunDefault();
            long stop = loop.NowInHighResolution;
            long duration = stop - start;
            double seconds = (double)duration / NanoSeconds;
            long ticksPerSecond = (long)Math.Floor(NumberOfTicks / seconds);
            Console.WriteLine($"Loop count : {TestHelper.Format(NumberOfTicks)} ticks in {TestHelper.Format(seconds)} seconds ({TestHelper.Format(ticksPerSecond)}/s).");
            
            this.idle.Dispose();
            loop.Dispose();
        }

        void OnIdleTickCallback(Idle handle)
        {
            this.ticks++;
            if (this.ticks >= NumberOfTicks)
            {
                handle.Stop();
            }
        }

        void RunTimed()
        {
            var loop = new Loop();

            this.idle = loop.CreateIdle();
            this.idle.Start(this.OnIdleTimedCallback);

            Timer timer = loop.CreateTimer();
            timer.Start(this.OnTimerCallback, 5000, 0);

            loop.RunDefault();
            double value = this.ticks / 0.5;
            Console.WriteLine($"Loop count timed : {TestHelper.Format(this.ticks)} ticks ({TestHelper.Format(value)} ticks/s).");

            this.idle.Dispose();
            loop.Dispose();
        }

        void OnTimerCallback(Timer timer)
        {
            this.idle.Stop();
            timer.Stop();
        }

        void OnIdleTimedCallback(Idle handle) => this.ticks++;

        public void Dispose() => this.idle = null;
    }
}
