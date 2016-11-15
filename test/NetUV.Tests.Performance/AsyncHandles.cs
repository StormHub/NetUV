// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NetUV.Core.Handles;

    sealed class AsyncHandles : IDisposable
    {
        const int PingCount = 1000 * 1000;
        readonly int threadCount;

        List<Thread> threads;
        Loop loop;

        class WorkContext : IDisposable
        {
            Async main;

            Loop loop;
            Async worker;
            int mainCount;
            int workerCount;

            public WorkContext(Loop mainLoop)
            {
                this.mainCount = 0;
                this.workerCount = 0;

                this.main = mainLoop.CreateAsync(this.OnMainCallback);

                this.loop = new Loop();
                this.worker = this.loop.CreateAsync(this.OnWorkerCallback);
            }

            public void Run()
            {
                this.main.Send();
                this.loop.RunDefault();
            }

            void OnMainCallback(Async handle)
            {
                this.mainCount++;
                if (this.worker.IsActive)
                {
                    this.worker.Send();
                }

                if (this.mainCount >= PingCount)
                {
                    this.main.CloseHandle(OnClose);
                }
            }

            void OnWorkerCallback(Async handle)
            {
                this.workerCount++;

                if (this.main.IsActive)
                {
                    this.main.Send();
                }

                if (this.workerCount >= PingCount)
                {
                    this.worker.CloseHandle(OnClose);
                }
            }

            static void OnClose(Async handle) => handle.Dispose();

            public void Dispose()
            {
                this.worker?.Dispose();
                this.worker = null;

                this.loop?.Dispose();
                this.loop = null;

                this.main?.Dispose();
                this.main = null;
            }
        }

        public AsyncHandles(int threadCount)
        {
            this.threadCount = threadCount;
            this.threads = new List<Thread>();
            this.loop = new Loop();
        }

        public void Run()
        {
            for (int i = 0; i < this.threadCount; i++)
            {
                var context = new WorkContext(this.loop);
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

            double totalTime = (double)time / TestHelper.NanoSeconds;
            double value = PingCount / totalTime;
            Console.WriteLine($"async{this.threadCount}: {TestHelper.Format(totalTime)} sec ({TestHelper.Format(value)}/sec)");
        }

        static void ThreadStart(object state)
        {
            var context = (WorkContext)state;
            context.Run();
            context.Dispose();
        }

        public void Dispose()
        {
            this.threads?.Clear();
            this.threads = null;

            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
