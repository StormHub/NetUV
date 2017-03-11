// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Threading;
    using NetUV.Core.Handles;
    using Timer = NetUV.Core.Handles.Timer;

    sealed class MillionAsync : IDisposable
    {
        const int Timeout = 5000;
        const int Count = 1024 * 1024;

        Loop loop;
        Async[] handles;
        Thread thread;
        int seed;
        int asyncEvents;
        int asyncSeen;
        ManualResetEventSlim resetEvent;

        public MillionAsync()
        {
            this.handles = new Async[Count];
            this.resetEvent = new ManualResetEventSlim(false);
            this.seed = 0;
            this.asyncEvents = 0;

            this.loop = new Loop();
        }

        public void Run()
        {
            for (int i = 0; i < Count; i++)
            {
                this.handles[i] = this.loop.CreateAsync(this.OnAsync);
            }

            this.loop
                .CreateTimer()
                .Start(this.OnTimer, Timeout, 0);
            this.thread = new Thread(this.ThreadStart)
            {
                IsBackground = true
            };
            this.thread.Start();

            this.loop.RunDefault();

            const double Seconds = (Timeout / 1000d);
            double value = this.asyncEvents / Seconds;
            Console.WriteLine($"Million async : {TestHelper.Format(this.asyncEvents)} async events in {TestHelper.Format(Seconds)} seconds ({TestHelper.Format(value)}/s, {TestHelper.Format(this.asyncSeen)} unique handles seen)");
        }

        void ThreadStart()
        {
            while (!this.resetEvent.IsSet)
            {
                int index = this.FastRandom() % Count;
                this.handles[index].Send();
            }
        }

        int FastRandom()
        {
            this.seed = this.seed * 214013 + 2531011;
            return Math.Abs(this.seed);
        }

        void OnTimer(Timer handle)
        {
            this.resetEvent.Set();
            this.thread.Join(1000);

            foreach (Async async in this.handles)
            {
                async.CloseHandle(OnClose);
                if (async.UserToken != null)
                {
                    this.asyncSeen++;
                }
            }

            handle.CloseHandle(OnClose);
        }

        void OnAsync(Async handle)
        {
            this.asyncEvents++;
            handle.UserToken = handle;
        }

        static void OnClose(ScheduleHandle handle) => handle.Dispose();

        public void Dispose()
        {
            this.handles = null;
            this.resetEvent.Dispose();
            this.resetEvent = null;
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
