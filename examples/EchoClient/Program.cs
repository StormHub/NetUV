// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace EchoClient
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
        static Loop loop;

        static string GetPipeName() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "\\\\?\\pipe\\echo"
            : "/tmp/echo";

        enum ServerType
        {
            Tcp,
            Pipe,
            Udp
        }

        public static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0
                    || !Enum.TryParse(args[0], true, out ServerType serverType))
                {
                    serverType = ServerType.Tcp;
                }

                RunLoop(serverType);

                loop.Dispose();
                loop = null;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Echo client error {exception}");
            }

            Console.WriteLine("Press any key to terminate the client");
            Console.ReadLine();
        }

        static void RunLoop(ServerType serverType)
        {
            loop = new Loop();

            IDisposable handle;
            var localEndPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            var remoteEndPoint = new IPEndPoint(IPAddress.Loopback, Port);
            switch (serverType)
            {
                case ServerType.Udp:
                    Udp udp = loop
                        .CreateUdp()
                        .ReceiveStart(OnReceive);
                    WritableBuffer buffer = CreateMessage();
                    udp.QueueSend(buffer, remoteEndPoint, OnSendCompleted);
                    handle = udp;
                    break;
                case ServerType.Pipe:
                    string name = GetPipeName();
                    handle = loop
                        .CreatePipe()
                        .ConnectTo(name, OnConnected);
                    break;
                default: // Default tcp
                    handle = loop
                        .CreateTcp()
                        .NoDelay(true)
                        .ConnectTo(localEndPoint, remoteEndPoint, OnConnected);
                    break;
            }

            Console.WriteLine($"{serverType}:Echo client loop starting.");
            loop.RunDefault();
            Console.WriteLine($"{serverType}:Echo client loop dropped out");
            handle?.Dispose();
        }

        static void OnReceive(Udp udp, IDatagramReadCompletion completion)
        {
            if (completion.Error != null)
            {
                Console.WriteLine($"Echo client receive error {completion.Error}");
            }

            IPEndPoint remoteEndPoint = completion.RemoteEndPoint;
            ReadableBuffer data = completion.Data;
            string message = data.ReadString(Encoding.UTF8);
            Console.WriteLine($"Echo client received : {message} from {remoteEndPoint}");
            udp.CloseHandle(OnClosed);
        }

        static void OnSendCompleted(Udp udp, Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine($"Echo server send error {exception}");
                udp.CloseHandle(OnClosed);
            }
        }

        static void OnConnected<T>(T client, Exception exception) 
            where T : StreamHandle
        {
            if (exception != null)
            {
                Console.WriteLine($"{typeof(T).Name}:Echo client error {exception}");
                client.CloseHandle(OnClosed);
                return;
            }

            Console.WriteLine($"{typeof(T).Name}:Echo client connected, request write message.");
            client.OnRead(OnAccept, OnError);

            WritableBuffer buffer = CreateMessage();
            client.QueueWriteStream(buffer, OnWriteCompleted);
        }

        static WritableBuffer CreateMessage()
        {
            byte[] array = Encoding.UTF8.GetBytes($"Greetings {DateTime.UtcNow}");
            WritableBuffer buffer = WritableBuffer.From(array);
            return buffer;
        }

        static void OnAccept(StreamHandle stream, ReadableBuffer data) 
        {
            if (data.Count == 0)
            {
                return;
            }

            string message = data.ReadString(Encoding.UTF8);
            data.Dispose();
            Console.WriteLine($"Echo client received : {message}");

            Console.WriteLine("Message received, sending QS to server");
            byte[] array = Encoding.UTF8.GetBytes("QS");
            WritableBuffer buffer = WritableBuffer.From(array);
            stream.QueueWriteStream(buffer, OnWriteCompleted);
        }

        static void OnWriteCompleted(StreamHandle stream, Exception error) 
        {
            if (error == null)
            {
                return;
            }

            Console.WriteLine($"Echo client write error {error}");
            stream.CloseHandle(OnClosed);
        }

        static void OnClosed(ScheduleHandle handle) => handle.Dispose();

        static void OnError(StreamHandle stream, Exception error)
            => Console.WriteLine($"Echo client read error {error}");
    }
}
