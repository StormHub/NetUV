// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    sealed class WorkerGroup : IDisposable
    {
        static readonly TimeSpan StartTimeout = TimeSpan.FromMilliseconds(500);
        readonly Worker[] workers;

        public WorkerGroup(string pipeName, List<ServerTcpContext> callbacks) 
            : this(Environment.ProcessorCount, pipeName, callbacks)
        {
        }

        public WorkerGroup(int eventLoopCount, string pipeName, List<ServerTcpContext> callbacks)
        {
            this.workers = new Worker[eventLoopCount];
            var terminationTasks = new Task[eventLoopCount];
            for (int i = 0; i < eventLoopCount; i++)
            {
                Worker worker;
                bool success = false;
                try
                {
                    worker = new Worker(pipeName, callbacks);
                    success = worker.StartAsync().Wait(StartTimeout);
                    if (!success)
                    {
                        throw new TimeoutException($"Connect to dispatcher pipe {pipeName} timed out.");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create {nameof(Worker)}.", ex);
                }
                finally
                {
                    if (!success)
                    {
                        Task.WhenAll(this.workers.Take(i).Select(workerLoop => workerLoop.ShutdownAsync())).Wait();
                    }
                }

                this.workers[i] = worker;
                terminationTasks[i] = worker.TerminationCompletion;
            }

            this.TerminationCompletion = Task.WhenAll(terminationTasks);
        }

        public Task TerminationCompletion { get; }

        public Task ShutdownGracefullyAsync()
        {
            foreach (Worker worker in this.workers)
            {
                worker.ShutdownAsync();
            }
            return this.TerminationCompletion;
        }

        public void Dispose()
        {
            this.ShutdownGracefullyAsync().Wait();
        }
    }
}
