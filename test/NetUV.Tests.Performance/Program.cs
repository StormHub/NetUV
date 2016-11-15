// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Linq;
    using NetUV.Core.Logging;

    public class Program
    {
        const string LoopCategory = "loop";
        const string TimerCategory = "timer";
        const string AsyncCategory = "async";
        const string TcpCategory = "tcp";
        const string PipeCategory = "pipe";
        const string UdpCategory = "udp";
        const string AddrInfoCategory = "addrinfo";

        const string Debug = "-debug";
        const string Pause = "-pause";

        public static void Main(string[] args)
        {
            string category = args.Length > 0 ? args[0] : null;
            string name = args.Length > 1 ? args[1] : null;

            bool pause = args.Length > 0
                && args.Any(x => string.Compare(x, Pause, StringComparison.OrdinalIgnoreCase) == 0);

            if (args.Length > 0 
                && args.Any(x => string.Compare(x, Debug, StringComparison.OrdinalIgnoreCase) == 0))
            {
                LogFactory.AddConsoleProvider();
            }

            Run(category, name);

            if (!pause)
            {
                return;
            }

            Console.WriteLine("Press any key to terminate the application");
            Console.ReadLine();
        }

        static void Run(string category, string name)
        {
            if (string.IsNullOrEmpty(category)
                || string.Compare(category, LoopCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunLoopBenchmark(name);
            }

            if (string.IsNullOrEmpty(category)
                || string.Compare(category, TimerCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunTimerBenchmark();
            }

            if (string.IsNullOrEmpty(category)
                || string.Compare(category, AsyncCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunAsyncBenchmark(name);
            }

            if (string.IsNullOrEmpty(category)
                || string.Compare(category, TcpCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunTcpBenchmark(name);
            }

            if (string.IsNullOrEmpty(category)
                || string.Compare(category, PipeCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunPipeBenchmark(name);
            }
            if (string.IsNullOrEmpty(category)
                || string.Compare(category, UdpCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunUdpBenchmark(name);
            }

            if (string.IsNullOrEmpty(category)
                || string.Compare(category, AddrInfoCategory, StringComparison.OrdinalIgnoreCase) == 0)
            {
                RunAddrInfoBenchmark();
            }
        }

        static void RunLoopBenchmark(string name)
        {
            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "count", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                using (var loopCount = new LoopCount())
                {
                    loopCount.Run();
                }
            }
        }

        static void RunTimerBenchmark()
        {
            using (var timers = new MillionTimers())
            {
                timers.Run();
            }
        }

        static void RunAsyncBenchmark(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                using (var millionAsync = new MillionAsync())
                {
                    millionAsync.Run();
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                using (var asyncThreads = new AsyncHandles(1))
                {
                    asyncThreads.Run();
                }

                using (var asyncThreads = new AsyncHandles(2))
                {
                    asyncThreads.Run();
                }

                using (var asyncThreads = new AsyncHandles(4))
                {
                    asyncThreads.Run();
                }

                using (var asyncThreads = new AsyncHandles(8))
                {
                    asyncThreads.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pummel", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                using (var asyncPummel = new AsyncPummel(1))
                {
                    asyncPummel.Run();
                }

                using (var asyncPummel = new AsyncPummel(2))
                {
                    asyncPummel.Run();
                }

                using (var asyncPummel = new AsyncPummel(4))
                {
                    asyncPummel.Run();
                }

                using (var asyncPummel = new AsyncPummel(8))
                {
                    asyncPummel.Run();
                }
            }
        }

        static void RunTcpBenchmark(string name)
        {
            if (string.IsNullOrEmpty(name) 
                || string.Compare(name, "writeBatch", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                using (var tcpWriteBatch = new TcpWriteBatch())
                {
                    tcpWriteBatch.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pingPong", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                using (var tcpPingPong = new TcpPingPong())
                {
                    tcpPingPong.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pump", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                using (var tcpPump = new Pump(HandleType.Tcp, 1))
                {
                    tcpPump.Run();
                }

                using (var tcpPump = new Pump(HandleType.Tcp, 100))
                {
                    tcpPump.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pound", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                using (var tcpPound = new Pound(HandleType.Tcp, 100))
                {
                    tcpPound.Run();
                }

                using (var tcpPound = new Pound(HandleType.Tcp, 1000))
                {
                    tcpPound.Run();
                }
            }
        }

        static void RunPipeBenchmark(string name)
        {
            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pump", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                using (var pipePump = new Pump(HandleType.Pipe, 1))
                {
                    pipePump.Run();
                }

                using (var pipePump = new Pump(HandleType.Pipe, 100))
                {
                    pipePump.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pound", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                using (var tcpPound = new Pound(HandleType.Pipe, 100))
                {
                    tcpPound.Run();
                }

                using (var tcpPound = new Pound(HandleType.Pipe, 1000))
                {
                    tcpPound.Run();
                }
            }
        }

        static void RunUdpBenchmark(string name)
        {
            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pummel", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                var pairs = new []
                {
                    new Tuple<int, int>(1, 1),
                    new Tuple<int, int>(10, 1),
                    new Tuple<int, int>(1, 100),
                    new Tuple<int, int>(1, 1000),
                    new Tuple<int, int>(10, 10),
                    new Tuple<int, int>(10, 100),
                    new Tuple<int, int>(10, 1000),
                    new Tuple<int, int>(100, 100),
                    new Tuple<int, int>(100, 1000),
                    new Tuple<int, int>(1000, 1000),
                };

                foreach (Tuple<int, int> pair in pairs)
                {
                    using (var udpPummel = new UdpPummel(pair.Item1, pair.Item2, 0))
                    {
                        udpPummel.Run();
                    }
                }

                foreach (Tuple<int, int> pair in pairs)
                {
                    using (var udpPummel = new UdpPummel(pair.Item1, pair.Item2, 5000)) // ms
                    {
                        udpPummel.Run();
                    }
                }
            }
        }

        static void RunAddrInfoBenchmark()
        {
            using (var requests = new GetAddrInfo())
            {
                requests.Run();
            }
        }
    }
}
