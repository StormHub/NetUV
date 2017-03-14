// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using NetUV.Core.Handles;

    sealed class MillionTimers : IDisposable
    {
        const int NumberOfTimers = 10 * 1000 * 1000;
        const long NanoSeconds = 1000000000;

        Loop loop;
        int timerCount;
        int closeCount;
        Timer[] timers;

        public MillionTimers()
        {
            this.loop = new Loop();
            this.timers = new Timer[NumberOfTimers];
        }

        public void Run()
        {
            this.timerCount = 0;
            this.closeCount = 0;

            int timeout = 0;

            long beforeAll = this.loop.NowInHighResolution;
            for (int i = 0; i < NumberOfTimers; i++)
            {
                if (i % 1000 == 0)
                {
                    timeout++;
                }
                this.timers[i] = this.loop.CreateTimer();
                this.timers[i].Start(this.OnTimer, timeout, 0);
            }

            long beforeRun = this.loop.NowInHighResolution;
            this.loop.RunDefault();
            long afterRun = this.loop.NowInHighResolution;

            foreach (Timer timer in this.timers)
            {
                timer.CloseHandle(this.OnClose);
            }

            this.loop.RunDefault();
            long afterAll = this.loop.NowInHighResolution;

            if (this.timerCount != NumberOfTimers)
            {
                Console.WriteLine($"Million timers : failed, expecting number of timer callbacks {NumberOfTimers}.");
            }
            else if (this.closeCount != NumberOfTimers)
            {
                Console.WriteLine($"Million timers : failed, expecting number of timer close {NumberOfTimers}.");
            }
            else
            {
                double value = (double)(afterAll - beforeAll) / NanoSeconds;
                Console.WriteLine($"Million timers : {TestHelper.Format(value)} seconds total.");

                value = (double)(beforeRun - beforeAll) / NanoSeconds;
                Console.WriteLine($"Million timers : {TestHelper.Format(value)} seconds init.");

                value = (double)(afterRun - beforeRun) / NanoSeconds;
                Console.WriteLine($"Million timers : {TestHelper.Format(value)} seconds dispatch.");

                value = (double)(afterAll - afterRun) / NanoSeconds;
                Console.WriteLine($"Million timers : {TestHelper.Format(value)} seconds cleanup.");
            }
        }

        void OnTimer(Timer timer) => this.timerCount++;

        void OnClose(Timer timer) => this.closeCount++;

        public void Dispose()
        {
            this.timers = null;
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
