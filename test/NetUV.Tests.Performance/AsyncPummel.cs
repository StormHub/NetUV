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
        Dictionary<Thread, WorkContext> threads;
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
            readonly Async aysnc;
            readonly Counter counter;

            public WorkContext(Async aysnc, Counter counter)
            {
                this.resetEvent = new ManualResetEventSlim(false);
                this.aysnc = aysnc;
                this.counter = counter;
            }

            public void Run()
            {
                while (!this.resetEvent.IsSet)
                {
                    if (this.counter.IsCompleted
                        || !this.aysnc.IsValid)
                    {
                        this.resetEvent.Wait();
                    }
                    else
                    {
                        this.aysnc.Send();
                    }
                }

                this.resetEvent.Dispose();
            }

            public void Close() => this.aysnc.CloseHandle(this.OnClose);

            void OnClose(Async handle)
            {
                handle.Dispose();
                if (!this.resetEvent.IsSet)
                {
                    this.resetEvent.Set();
                }
            }
        }

        public AsyncPummel(int threadCount)
        {
            this.threadCount = threadCount;
            this.threads = new Dictionary<Thread, WorkContext>();
            this.counter = new Counter();
            this.loop = new Loop();
        }

        public void Run()
        {
            for (int i = 0; i < this.threadCount; i++)
            {
                Async handle = this.loop.CreateAsync(this.OnAsync);
                var context = new WorkContext(handle, this.counter);
                var thread = new Thread(ThreadStart);
                this.threads.Add(thread, context);
                thread.Start(context);
            }

            long time = this.loop.NowInHighResolution;
            this.loop.RunDefault();

            foreach (Thread thread in this.threads.Keys)
            {
                thread.Join(Timeout);
            }

            time = this.loop.NowInHighResolution - time;
            long count = this.counter.Count;
            double totalTime = (double)time / TestHelper.NanoSeconds;
            double value = count / totalTime;
            Console.WriteLine($"Async pummel {this.threadCount}: {TestHelper.Format(count)} callbacks in {TestHelper.Format(totalTime)} sec ({TestHelper.Format(value)}/sec)");
        }

        void OnAsync(Async handle)
        {
            if (this.counter.Increment())
            {
                foreach (WorkContext context in this.threads.Values)
                {
                    context.Close();
                }
            }
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
