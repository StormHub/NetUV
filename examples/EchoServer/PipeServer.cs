// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace EchoServer
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;

    sealed class PipeServer : IServer
    {
        readonly Loop loop;
        readonly string name;
        Pipe server;

        public PipeServer() : this(GetPipeName())
        {
        }

        internal PipeServer(string name)
        {
            this.loop = new Loop();
            this.name = name;
        }

        public void Run()
        {
            this.server = this.loop
                .CreatePipe()
                .Listen(this.name, this.OnConnection);
            Console.WriteLine($"{nameof(PipeServer)} started on {this.name}.");

            this.loop.RunDefault();
            Console.WriteLine($"{nameof(PipeServer)} loop completed.");
        }

        void OnConnection(Pipe client, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{nameof(PipeServer)} client connection error {error}");
                client.CloseHandle(OnClosed);
                return;
            }

            Console.WriteLine($"{nameof(PipeServer)} client connection accepted {client.GetPeerName()}");
            client.OnRead(this.OnAccept, OnError);
        }

        void OnAccept(Pipe pipe, ReadableBuffer data)
        {
            string message = data.ReadString(Encoding.UTF8);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            Console.WriteLine($"{nameof(PipeServer)} received : {message}");

            //
            // Scan for the letter Q which signals that we should quit the server.
            // If we get QS it means close the stream.
            //
            if (message.StartsWith("Q"))
            {
                Console.WriteLine($"{nameof(PipeServer)} closing client.");
                pipe.CloseHandle(OnClosed);

                if (!message.EndsWith("QS"))
                {
                    return;
                }

                Console.WriteLine($"{nameof(PipeServer)} shutting down.");
                this.server.Dispose();
            }
            else
            {
                Console.WriteLine($"{nameof(PipeServer)} sending echo back.");
                WritableBuffer buffer = pipe.Allocate();
                buffer.WriteString($"ECHO [{message}]", Encoding.UTF8);
                pipe.QueueWriteStream(buffer, (handle, exception) =>
                {
                    OnWriteCompleted(handle, exception);
                    buffer.Dispose();
                });
            }
        }

        static void OnWriteCompleted(Pipe pipe, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{nameof(PipeServer)} write error {error}");
                pipe.CloseHandle(OnClosed);
            }
        }

        static void OnClosed(Pipe handle) => handle.Dispose();

        static void OnError(Pipe pipe, Exception error) => 
            Console.WriteLine($"{nameof(PipeServer)} client read error {error}");

        public void Dispose() => this.loop.Dispose();

        static string GetPipeName() => 
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "\\\\?\\pipe\\echo" 
            : "/tmp/echo";
    }
}
