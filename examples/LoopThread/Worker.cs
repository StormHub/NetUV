// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoopThread
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;

    sealed class Worker : IDisposable
    {
        readonly EventLoop eventLoop;
        readonly string pipeName;

        public Worker(string pipeName)
        {
            this.pipeName = pipeName;
            this.eventLoop = new EventLoop();
        }

        public Task StartAsync()
        {
            Task task = this.eventLoop.ExecuteAsync(state => ((Loop)state)
                    .CreatePipe(true) // use IPC 
                    .ConnectTo(this.pipeName, this.OnConnected),
                this.eventLoop.Loop);
            return task;
        }

        public Task TerminationCompletion => this.eventLoop.TerminationCompletion;

        void OnConnected(Pipe pipe, Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"{nameof(Worker)} failed to connect to {this.pipeName}");
                pipe.CloseHandle(OnClosed);
            }
            else
            {
                Console.WriteLine($"{nameof(Worker)} connected to {this.pipeName}");
                pipe.OnRead(this.GetPendingHandle);
            }
        }

        void GetPendingHandle(Pipe pipe, IStreamReadCompletion completion)
        {
            if (completion.Error != null || completion.Completed)
            {
                if (completion.Error != null)
                {
                    Console.WriteLine($"{nameof(Worker)} read pending handle error {completion.Error}");
                }
                pipe.CloseHandle(OnClosed);
                return;
            }

            var tcp = (Tcp)pipe.CreatePendingType();
            tcp.OnRead(this.OnAccept, OnError);
        }

        void OnAccept(Tcp tcp, ReadableBuffer data)
        {
            string message = data.ReadString(Encoding.UTF8);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Console.WriteLine($"{nameof(Worker)} received : {message}");

            if (message.StartsWith("Q"))
            {
                Console.WriteLine($"{nameof(Worker)} closing tcp client.");
                tcp.CloseHandle(OnClosed);
            }
            else
            {
                Console.WriteLine($"{nameof(Worker)} sending echo back.");

                WritableBuffer buffer = tcp.Allocate();
                buffer.WriteString($"ECHO [{message}]", Encoding.UTF8);
                tcp.QueueWriteStream(buffer, (handle, exception) =>
                {
                    buffer.Dispose();
                    OnWriteCompleted(handle, exception);
                });
            }
        }

        static void OnWriteCompleted(Tcp handle, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{nameof(Worker)} server write error {error}");
                handle.CloseHandle(OnClosed);
            }
        }

        static void OnError(Tcp handle, Exception error)
            => Console.WriteLine($"{nameof(Worker)} read error {error}");

        public Task ShutdownAsync() => this.eventLoop.ShutdownGracefullyAsync();

        static void OnClosed(StreamHandle handle) => handle.Dispose();

        public void Dispose()
        {
             this.ShutdownAsync().Wait();
        }
    }
}
