// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoopThread
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;
    using NetUV.Core.Logging;
    using NetUV.Core.Utilities;

    class Program
    {
        const int Port = 9988;

        static int SessionIDSeed = 1;
        static readonly AttributeKey<string> SESSION_ID = AttributeKey<string>.ValueOf("SESSION_ID");

        public static void Main(string[] args)
        {
            LogFactory.AddConsoleProvider(LogLevel.Debug);

            Dispatcher dispatcher = null;
            try
            {
                // Start dispatcher loop to listen on pipe
                dispatcher = new Dispatcher();
                if (!dispatcher.StartAsync(Environment.ProcessorCount).Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException($"Dispather pipe listening on {dispatcher.PipeName} timed out on");
                }

                // Start tcp server on dispatcher
                var endPoint = new IPEndPoint(IPAddress.Any, Port);
                var tcp = dispatcher.Listen(endPoint, OnAccept, OnRead, OnError);
                tcp.GetAttribute(SESSION_ID).Set(GetNewID());

                Console.WriteLine("Press any key to terminate the program.");
                Console.ReadLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Loop thread error {exception}.");
            }
            finally
            {
                dispatcher?.Dispose();
            }

            dispatcher?.TerminationCompletion.Wait();
        }

        static void OnAccept(Tcp serverTcp, Tcp newTcp)
        {
            //init some attributes here
            newTcp.GetAttribute(SESSION_ID).Set(GetNewID());
            Console.WriteLine($"AcceptNewTcpSession:{newTcp.GetAttribute(SESSION_ID).Get()}, "
                + $"FromTcpSession:{serverTcp.GetAttribute(SESSION_ID).Get()}");
        }

        static void OnRead(Tcp tcp, ReadableBuffer data)
        {
            WritableBuffer buffer = tcp.Allocate();
            var message = data.ReadString(Encoding.UTF8);
            buffer.WriteString($"ECHO [{message}]", Encoding.UTF8);
            tcp.QueueWriteStream(buffer, (handle, exception) =>
            {
                buffer.Dispose();
                OnWriteCompleted(handle, exception);
            });
        }

        static void OnWriteCompleted(Tcp handle, Exception error)
        {
            if (error != null)
            {
                Console.WriteLine($"{nameof(Tcp)} server write error {error}");
                handle.CloseHandle(OnClosed);
            }
        }

        static void OnClosed(Tcp handle) => handle.Dispose();

        static void OnError(Tcp tcp, Exception e) => Console.WriteLine($"{nameof(Tcp)} read error {e}");

        static string GetNewID()
        {
            var id = Interlocked.Add(ref SessionIDSeed, 1);
            return id.ToString();
        }
    }
}