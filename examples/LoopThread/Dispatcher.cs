// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoopThread
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;

    sealed class Dispatcher : IDisposable
    {
        readonly EventLoop eventLoop;
        readonly List<Pipe> pipes;
        readonly WritableBuffer ping;
        readonly WindowsApi windowsApi;
        int requestId;

        public Dispatcher()
        {
            this.PipeName = GetPipeName();
            this.eventLoop = new EventLoop();
            this.pipes = new List<Pipe>();
            this.ping = WritableBuffer.From(Encoding.UTF8.GetBytes("PING"));
            this.windowsApi = new WindowsApi();
            this.requestId = 0;
        }

        public string PipeName { get; }

        public Task StartAsync()
        {
            // Starts the Pipe to dispatch handles
            Task task = this.eventLoop.ExecuteAsync(state => ((Loop)state)
                    .CreatePipe()
                    .Bind(this.PipeName)
                    .Listen(this.OnConnection, true), // Use IPC for clients
                this.eventLoop.Loop);
            return task;
        }

        public Task ListenOnAsync(IPEndPoint endPoint)
        {
            Task task = this.eventLoop.ExecuteAsync(state => ((Loop)state)
                    .CreateTcp()
                    .SimultaneousAccepts(true)
                    .Listen(endPoint, this.Dispatch), 
                this.eventLoop.Loop);
            return task;
        }

        void Dispatch(Tcp tcp, Exception exception)
        {
            if (exception != null)
            {
                tcp.CloseHandle(OnClosed);
                Console.WriteLine($"{nameof(Dispatcher)} client tcp connection error {exception}");
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

            pipe.QueueWriteStream(this.ping, tcp, (handle, error) =>
            {
                tcp.CloseHandle(OnClosed);
                this.DispatchCompleted(handle, error);
            });
        }

        void DispatchCompleted(Pipe pipe, Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"{nameof(Dispatcher)} dispatch client tcp connection error {exception}");
            }
            else
            {
                Console.WriteLine($"{nameof(Dispatcher)} client tcp dispatched to {pipe.GetPeerName()}");
            }
        }

        public Task TerminationCompletion => this.eventLoop.TerminationCompletion;

        void OnConnection(Pipe pipe, Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"{nameof(Dispatcher)} pipe client connection error {exception}");
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
