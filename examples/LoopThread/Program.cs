// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoopThread
{
    using System;
    using System.Net;
    using Microsoft.Extensions.Logging;
    using NetUV.Core.Logging;

    class Program
    {
        const int Port = 9988;

        public static void Main(string[] args)
        {
            LogFactory.AddConsoleProvider(LogLevel.Debug);

            Dispatcher dispatcher = null;
            WorkerGroup workerGroup = null;
            try
            {
                // Start dispatcher loop to listen on pipe
                dispatcher = new Dispatcher();
                if (!dispatcher.StartAsync().Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException($"Dispather pipe listening on {dispatcher.PipeName} timed out on");
                }

                // Start work groups and connect to the dispatcher pipe
                Console.WriteLine($"Dispather pipe started on {dispatcher.PipeName}");
                workerGroup = new WorkerGroup(dispatcher.PipeName);

                // Start tcp server on dispatcher
                var endPoint = new IPEndPoint(IPAddress.Loopback, Port);
                if (!dispatcher.ListenOnAsync(endPoint).Wait(TimeSpan.FromSeconds(10)))
                {
                    throw new TimeoutException($"Dispather tcp listening on {endPoint} timed out");
                }

                Console.WriteLine("Press any key to terminate the program.");
                Console.ReadLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Loop thread error {exception}.");
            }
            finally
            {
                workerGroup?.Dispose();
                dispatcher?.Dispose();
            }

            dispatcher?.TerminationCompletion.Wait();
        }
    }
}