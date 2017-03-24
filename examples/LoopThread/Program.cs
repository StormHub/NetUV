// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoopThread
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using NetUV.Core.Buffers;
    using NetUV.Core.Channels;
    using NetUV.Core.Handles;
    using NetUV.Core.Logging;

    class Program
    {
        const int Port = 9988;
        static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, Port);
        static EventLoop eventLoop;

        public static void Main(string[] args)
        {
            LogFactory.AddConsoleProvider(LogLevel.Debug);

            try
            {
                eventLoop = new EventLoop();
                Task task = Task.Run(async () => await RunLoopAsync());

                eventLoop.Schedule(loop => loop
                    .CreateTcp()
                    .SimultaneousAccepts(true)
                    .Listen(EndPoint, OnConnection));

                Console.WriteLine("Waiting for loop to complete.");
                task.Wait();
                Console.WriteLine("Loop completed.");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Loop thread error {exception}.");
            }
            finally
            {
                eventLoop?.Dispose();
            }

            Console.WriteLine("Press any key to terminate the program.");
            Console.ReadLine();
        }

        static void OnConnection(Tcp client, Exception error)
        {
            IStream stream = client.CreateStream();
            stream.Subscribe(OnNext, OnError, OnCompleted);
        }

        static void OnNext(IStream stream, ReadableBuffer data)
        {
            string message = data.Count > 0 ? data.ReadString(data.Count, Encoding.UTF8) : null;
            data.Dispose();
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Console.WriteLine($"Server received : {message}");
            //
            // Scan for the letter Q which signals that we should quit the server.
            // If we get QS it means close the stream.
            //
            if (message.StartsWith("Q"))
            {
                Console.WriteLine("Server closing stream.");
                stream.Dispose();

                if (!message.EndsWith("QS"))
                {
                    return;
                }

                Console.WriteLine("Server shutting down.");
                eventLoop.ScheduleStop();
            }
            else
            {
                Console.WriteLine("Server sending echo back.");
                byte[] array = Encoding.UTF8.GetBytes($"ECHO [{message}]");
                WritableBuffer buffer = WritableBuffer.From(array);
                stream.Write(buffer, OnWriteCompleted);
            }
        }

        static void OnWriteCompleted(IStream stream, Exception error)
        {
            if (error == null)
            {
                return;
            }

            Console.WriteLine($"Server write error {error}");
            stream.Handle.CloseHandle(OnClosed);
        }

        static void OnError(IStream stream, Exception error) => Console.WriteLine($"Server read error {error}");

        static void OnCompleted(IStream stream) => stream.Handle.CloseHandle(OnClosed);

        static void OnClosed(ScheduleHandle handle) => handle.Dispose();

        static async Task RunLoopAsync()
        {
            Console.WriteLine("Starting event loop.");

            await eventLoop.RunAsync();

            Console.WriteLine("Event loop completed");
            eventLoop.Dispose();
        }
    }
}