// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;
    using NetUV.Core.Logging;

    public sealed class Dispatcher : IDisposable
    {
        static readonly ILog Logger = LogFactory.ForContext<EventLoop>();

        readonly EventLoop eventLoop;
        readonly List<Pipe> pipes;
        readonly WindowsApi windowsApi;
        readonly List<ServerTcpContext> callbacks;
        int requestId;
        WorkerGroup workerGroup;

        public Dispatcher()
        {
            this.PipeName = GetPipeName();
            this.eventLoop = new EventLoop();
            this.pipes = new List<Pipe>();
            this.windowsApi = new WindowsApi();
            this.callbacks = new List<ServerTcpContext>();
            this.requestId = 0;
        }

        public string PipeName { get; }

        public Task StartAsync(int workerCount)
        {
            // Starts the Pipe to dispatch handles
            Task task = this.eventLoop.ExecuteAsync(state => ((Loop)state)
                    .CreatePipe()
                    .Bind(this.PipeName)
                    .Listen(this.OnConnection, true), // Use IPC for clients
                this.eventLoop.Loop);

            this.StartWorker(workerCount);
            return task;
        }

        void StartWorker(int workerCount)
        {
            this.workerGroup = new WorkerGroup(workerCount, this.PipeName, this.callbacks);
        }

        public Tcp Listen(IPEndPoint endPoint,
                                    Action<Tcp, Tcp> onAccept,
                                    Action<Tcp, ReadableBuffer> onRead,
                                    Action<Tcp, Exception> onError)
        {
            Contract.Requires(onAccept != null);
            Contract.Requires(onRead != null);
            Contract.Requires(onError != null);

            var tcp = this.eventLoop.Loop
                .CreateTcp()
                .SimultaneousAccepts(true);

            var cb = new ServerTcpContext(tcp, onAccept, onRead, onError);
            this.callbacks.Add(cb);
            tcp.Listen(endPoint, (newTcp, exception) => this.Dispatch(newTcp, cb.Hello, exception));

            return tcp;
        }

        void Dispatch(Tcp tcp, WritableBuffer ping, Exception exception)
        {
            if (exception != null)
            {
                tcp.CloseHandle(OnClosed);
                Logger.Error($"{nameof(Dispatcher)} client tcp connection error {exception}");
                return;
            }

            int count = this.pipes.Count;
            if (count == 0)
            {
                tcp.CloseHandle(OnClosed);
                throw new InvalidOperationException("No pipe connections to dispatch handles.");
            }

            int id = Interlocked.Increment(ref this.requestId);
            Pipe pipe = this.pipes[Math.Abs(id % count)];

            this.windowsApi.DetachFromIOCP(tcp);

            pipe.QueueWriteStream(ping, tcp, (handle, error) =>
            {
                tcp.CloseHandle(OnClosed);
                this.DispatchCompleted(handle, error);
            });
        }

        void DispatchCompleted(Pipe pipe, Exception exception)
        {
            if (exception != null)
            {
                Logger.Error($"{nameof(Dispatcher)} dispatch client tcp connection error {exception}");
            }
            else
            {
                Logger.Debug($"{nameof(Dispatcher)} client tcp dispatched to {pipe.GetPeerName()}");
            }
        }

        public Task TerminationCompletion => this.eventLoop.TerminationCompletion;

        void OnConnection(Pipe pipe, Exception exception)
        {
            if (exception != null)
            {
                Logger.Error($"{nameof(Dispatcher)} pipe client connection error {exception}");
                pipe.CloseHandle(OnClosed);
            }
            else
            {
                pipe.OnRead(this.OnRead);
                this.pipes.Add(pipe);
            }
        }

        void OnRead(Pipe pipe, IStreamReadCompletion data)
        {
            // Dispatcher client pipes do not need to read anything,
            // It writes tcp handles to the clients. 
            // If client connection completed, removes it from the pipes.
            if (data.Completed)
            {
                if (this.pipes.Contains(pipe))
                {
                    this.pipes.Remove(pipe);
                }
                pipe.CloseHandle(OnClosed);
            }
        }

        static void OnClosed(StreamHandle handle) => handle.Dispose();

        public void Dispose()
        {
            this.eventLoop.ShutdownGracefullyAsync().Wait();
            this.workerGroup.Dispose();
        }

        public static string GetPipeName()
        {
            string pipeName = "NetUV_" + Guid.NewGuid().ToString("n");
            return (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? @"\\.\pipe\"
                : "/tmp/") + pipeName;
        }
    }
}
