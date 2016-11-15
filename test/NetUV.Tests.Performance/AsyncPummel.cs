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
        readonly int threadCount;
        List<Thread> threads;
        Loop loop;
        Counter counter;

        class Counter
        {
            public Counter()
            {
                this.Count = 0;
            }

            public bool IsCompleted => this.Count >= PingCount;

            public int Count { get; private set; }

            public void Increment() => this.Count++;
        }

        class WorkContext : IDisposable
        {
            Counter counter;
            long state;
            Async aysnc;

            public WorkContext(Loop loop, Counter counter)
            {
                this.counter = counter;
                this.state = 0;
                this.aysnc = loop.CreateAsync(this.OnCallback);
            }

            public void Run()
            {
                while (Interlocked.Read(ref this.state) == 0) // Running
                {
                    this.aysnc.Send();
                }

                Interlocked.Exchange(ref this.state, 2); // Stopped
            }

            static void OnClose(Async handle) => handle.Dispose();

            void OnCallback(Async handle)
            {
                this.counter.Increment();

                if (this.counter.IsCompleted)
                {
                    Interlocked.Exchange(ref this.state, 1); // Stopping

                    while (Interlocked.Read(ref this.state) != 2) 
                    {
                        // wait for stopped
                    }
                    this.aysnc.CloseHandle(OnClose);
                }
            }

            public void Dispose()
            {
                this.aysnc = null;
                this.counter = null;
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
                var thread = new Thread(ThreadStart);
                this.threads.Add(thread);
                thread.Start(context);
            }

            long time = this.loop.NowInHighResolution;
            this.loop.RunDefault();

            foreach (Thread thread in this.threads)
            {
                thread.Join();
            }

            time = this.loop.NowInHighResolution - time;
            int count = this.counter.Count;
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
