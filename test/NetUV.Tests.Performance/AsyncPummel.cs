// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NetUV.Core.Handles;

    sealed class AsyncPummel : IDisposable
    {
        const int PingCount = 1000 * 1000;
        const int Timeout = 5000;

        readonly int threadCount;
        List<Thread> threads;
        Loop loop;
        Counter counter;

        class Counter
        {
            long count;

            public Counter()
            {
                this.count = 0;
            }

            public long Count => this.count;

            public bool Increment() => Interlocked.Increment(ref this.count) >= PingCount;

            public bool IsCompleted => Interlocked.Read(ref this.count) >= PingCount;
        }

        class WorkContext
        {
            readonly ManualResetEventSlim resetEvent;

            readonly Counter counter;
            readonly Async aysnc;

            public WorkContext(Loop loop, Counter counter)
            {
                this.resetEvent = new ManualResetEventSlim(false);
                this.counter = counter;
                this.aysnc = loop.CreateAsync(this.OnCallback);
            }

            public void Run()
            {
                while (!this.counter.IsCompleted)
                {
                    this.aysnc.Send();
                }

                this.resetEvent.Wait();
            }

            void OnClose(Async handle)
            {
                handle.Dispose();
                this.resetEvent.Set();
            } 

            void OnCallback(Async handle)
            {
                if (!this.counter.Increment())
                {
                    return;
                }

                this.aysnc.CloseHandle(this.OnClose);
            }
        }

        public AsyncPummel(int threadCount)
        {
            this.threadCount = threadCount;
            this.threads = new List<Thread>();
            this.counter = new Counter();
            this.loop = new Loop();
        }

        public void Run()
        {
            for (int i = 0; i < this.threadCount; i++)
            {
                var context = new WorkContext(this.loop, this.counter);
                var thread = new Thread(ThreadStart)
                {
                    IsBackground = true
                };
                this.threads.Add(thread);
                thread.Start(context);
            }

            long time = this.loop.NowInHighResolution;
            this.loop.RunDefault();

            foreach (Thread thread in this.threads)
            {
                thread.Join(Timeout);
            }

            time = this.loop.NowInHighResolution - time;
            long count = this.counter.Count;
            double totalTime = (double)time / TestHelper.NanoSeconds;
            double value = count / totalTime;
            Console.WriteLine($"Async pummel {this.threadCount}: {TestHelper.Format(count)} callbacks in {TestHelper.Format(totalTime)} sec ({TestHelper.Format(value)}/sec)");
        }

        static void ThreadStart(object state)
        {
            var context = (WorkContext)state;
            context.Run();
        }

        public void Dispose()
        {
            this.threads.Clear();
            this.threads = null;

            this.counter = null;
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
