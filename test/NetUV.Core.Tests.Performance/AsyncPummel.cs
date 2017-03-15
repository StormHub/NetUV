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
        List<WorkContext> threads;
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
            readonly Async handle;
            readonly Counter counter;

            public WorkContext(Async handle, Counter counter)
            {
                this.handle = handle;
                this.counter = counter;
                this.resetEvent = new ManualResetEventSlim(false);
            }

            public void Run()
            {
                while (!this.resetEvent.IsSet)
                {
                    if (this.counter.IsCompleted)
                    {
                        break;
                    }

                    this.handle.Send();
                }

                this.resetEvent.Wait();
            }

            public void Close() => this.handle.CloseHandle(this.OnClose);

            void OnClose(Async asyncHandle)
            {
                asyncHandle.Dispose();
                this.resetEvent.Set();
            }
        }

        public AsyncPummel(int threadCount)
        {
            this.threadCount = threadCount;
            this.threads = new List<WorkContext>();
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
                this.threads.Add(context);
                thread.Start(context);
            }

            long time = this.loop.NowInHighResolution;
            this.loop.RunDefault();
            time = this.loop.NowInHighResolution - time;

            long count = this.counter.Count;
            double totalTime = (double)time / TestHelper.NanoSeconds;
            double value = count / totalTime;
            Console.WriteLine($"Async pummel {this.threadCount}: {TestHelper.Format(count)} callbacks in {TestHelper.Format(totalTime)} sec ({TestHelper.Format(value)}/sec)");
        }

        void OnAsync(Async handle)
        {
            if (!this.counter.Increment())
            {
                return;
            }

            foreach (WorkContext context in this.threads)
            {
                context.Close();
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
