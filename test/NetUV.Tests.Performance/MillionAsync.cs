// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NetUV.Core.Handles;
    using Timer = NetUV.Core.Handles.Timer;

    sealed class MillionAsync : IDisposable
    {
        const int Timeout = 5000;
        const int Count = 1024 * 1024;

        Loop loop;
        Dictionary<Async, int> handles;
        Thread thread;
        int done;
        int seed;
        int asyncEvents;

        public MillionAsync()
        {
            this.handles = new Dictionary<Async, int>();

            this.done = 0;
            this.seed = 0;
            this.asyncEvents = 0;

            this.loop = new Loop();
        }

        public void Run()
        {
            for (int i = 0; i < Count; i++)
            {
                Async handle = this.loop
                    .CreateAsync(this.OnAsync);
                this.handles.Add(handle, 0);
            }

            this.loop
                .CreateTimer()
                .Start(this.OnTimer, Timeout, 0);
            this.thread = new Thread(this.ThreadStart);
            this.thread.Start();

            this.loop.RunDefault();

            int handleSeen = this.handles.Values.Count(x => x > 0);
            const double Seconds = (Timeout / 1000d);
            double value = this.asyncEvents / Seconds;
            Console.WriteLine($"Million async : {TestHelper.Format(this.asyncEvents)} async events in {TestHelper.Format(Seconds)} seconds ({TestHelper.Format(value)}/s, {TestHelper.Format(handleSeen)} unique handles seen)");
        }

        void ThreadStart()
        {
            Async[] array = this.handles.Keys.ToArray();
            while (this.done == 0)
            {
                int index = this.FastRandom() % Count;
                array[index].Send();
            }

            this.done = 2;
        }

        int FastRandom()
        {
            this.seed = this.seed * 214013 + 2531011;
            return Math.Abs(this.seed);
        }

        void OnTimer(Timer handle)
        {
            this.done = 1;
            while (this.done != 2) { /* Wait for thread to exit */ }

            foreach (Async async in this.handles.Keys)
            {
                async.CloseHandle(OnClose);
            }

            handle.CloseHandle(OnClose);
        }

        void OnAsync(Async handle)
        {
            this.asyncEvents++;
            this.handles[handle] = 1;
        } 

        static void OnClose(ScheduleHandle handle) => handle.Dispose();

        public void Dispose()
        {
            this.handles?.Clear();
            this.handles = null;

            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
