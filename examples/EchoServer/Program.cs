// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace EchoServer
{
    using System;
    using NetUV.Core.Logging;

    public class Program
    {
        const int Port = 9988;

        enum ServerType
        {
            Tcp,
            Pipe,
            Udp
        }

        public static void Main(string[] args)
        {
            LogFactory.AddConsoleProvider();

            try
            {
                if (args.Length == 0
                    || !Enum.TryParse(args[0], true, out ServerType serverType))
                {
                    serverType = ServerType.Tcp;
                }

                Run(serverType);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Echo server error {exception}.");
            }

            Console.WriteLine("Press any key to terminate echo server.");
            Console.ReadLine();
        }

        static void Run(ServerType serverType)
        {
            IServer server = null;
            try
            {
                switch (serverType)
                {
                    case ServerType.Udp:
                        server = new UdpServer(Port);
                        break;
                    case ServerType.Pipe:
                        server = new PipeServer();
                        break;
                    default: // Default to tcp
                        server = new TcpServer(Port);
                        break;
                }

                server.Run();
            }
            finally 
            {
                server?.Dispose();
            }
        }
    }
}
