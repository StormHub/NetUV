// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace EchoServer
{
    using System;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using NetUV.Core.Buffers;
    using NetUV.Core.Handles;
    using NetUV.Core.Logging;

    public class Program
    {
        const int Port = 9988;
        static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, Port);

        static Loop loop;
        static IDisposable server;

        enum ServerType
        {
            Tcp,
            Pipe,
            Udp
        }

        static string GetPipeName() => 
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "\\\\?\\pipe\\echo" 
            : "/tmp/echo";

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

                StartServer(serverType);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Echo server error {exception}.");
            }
            finally
            {
                loop.Dispose();
            }

            Console.WriteLine("Press any key to terminate echo server.");
            Console.ReadLine();
        }

        static void StartServer(ServerType serverType)
        {
            loop = new Loop();

            switch (serverType)
            {
                case ServerType.Udp:
                    var endPoint = new IPEndPoint(IPAddress.Any, Port);
                    server = loop
                        .CreateUdp()
                        .Bind(endPoint)
                        .MulticastLoopback(true)
                        .ReceiveStart(OnReceive);
                    Console.WriteLine($"{serverType}:Echo server receive started.");
                    break;
                case ServerType.Pipe:
                    string name = GetPipeName();
                    server = loop
                        .CreatePipe()
                        .Listen(name, OnConnection);
                    Console.WriteLine($"{serverType}:Echo server started on {name}.");
                    break;
                default: // Default to tcp
                    server = loop
                        .CreateTcp()
                        .SimultaneousAccepts(true)
                        .Listen(EndPoint, OnConnection);
                    Console.WriteLine($"{serverType}:Echo server started on {EndPoint}.");
                    break;
            }

            loop.RunDefault();
            Console.WriteLine($"{serverType}Echo server loop completed.");
        }

        static void OnReceive(Udp udp, IDatagramReadCompletion completion)
        {
            if (completion.Error != null)
            {
                Console.WriteLine($"Echo server receive error {completion.Error}");
                udp.CloseHandle(OnClosed);
                return;
            }

            IPEndPoint remoteEndPoint = completion.RemoteEndPoint;
            ReadableBuffer data = completion.Data;
            if (data.Count == 0)
            {
                return;
            }
            string message = data.ReadString(data.Count, Encoding.UTF8);
            Console.WriteLine($"Echo server received : {message} from {remoteEndPoint}");

            Console.WriteLine($"Echo server sending echo back to {remoteEndPoint}.");
            byte[] array = Encoding.UTF8.GetBytes($"ECHO [{message}]");
            WritableBuffer buffer = WritableBuffer.From(array);
            udp.QueueSend(buffer, remoteEndPoint, OnSendCompleted);
        }

        static void OnSendCompleted(Udp udp, Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"Echo server send error {exception}");
            }
            udp.CloseHandle(OnClosed);
        }

        static void OnConnection<T>(T client, Exception error)
            where T : StreamHandle
        {
            if (error != null)
            {
                Console.WriteLine($"{typeof(T).Name}:Echo server client connection failed {error}");
                client.CloseHandle(OnClosed);
                return;
            }

            var tcp = client as Tcp;
            if (tcp != null)
            {
                Console.WriteLine($"{typeof(T).Name}:Echo server client connection accepted {tcp.GetPeerEndPoint()}");
            }

            var pipe = client as Pipe;
            if (pipe != null)
            {
                Console.WriteLine($"{typeof(T).Name}:Echo server client connection accepted {pipe.GetPeerName()}");
            }

            client.OnRead(OnAccept, OnError);
        }

        static void OnAccept(StreamHandle stream, ReadableBuffer data) 
        {
            string message = data.Count > 0 ? data.ReadString(data.Count, Encoding.UTF8) : null;
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Console.WriteLine($"Echo server received : {message}");

            //
            // Scan for the letter Q which signals that we should quit the server.
            // If we get QS it means close the stream.
            //
            if (message.StartsWith("Q"))
            {
                Console.WriteLine("Echo server closing stream.");
                stream.Dispose();

                if (!message.EndsWith("QS"))
                {
                    return;
                }

                Console.WriteLine("Echo server shutting down.");
                server.Dispose();
            }
            else
            {
                Console.WriteLine("Echo server sending echo back.");
                byte[] array = Encoding.UTF8.GetBytes($"ECHO [{message}]");
                WritableBuffer buffer = WritableBuffer.From(array);
                stream.QueueWriteStream(buffer, OnWriteCompleted);
            }
        }

        static void OnWriteCompleted(StreamHandle stream, Exception error) 
        {
            if (error == null)
            {
                return;
            }

            Console.WriteLine($"Echo server write error {error}");
            stream.CloseHandle(OnClosed);
        }

        static void OnClosed(ScheduleHandle handle) => handle.Dispose();

        static void OnError(StreamHandle stream, Exception error) 
            => Console.WriteLine($"Echo server read error {error}");
    }
}
