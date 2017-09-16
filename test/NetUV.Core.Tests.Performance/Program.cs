﻿// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using NetUV.Core.Common;
    using NetUV.Core.Handles;
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

        static bool isDebug;
        static bool shouldPause;
        static int processorCount;

        public static void Main(string[] args)
        {
            processorCount = Environment.ProcessorCount;
            Console.WriteLine(
                  $"\n {RuntimeInformation.OSArchitecture} {RuntimeInformation.OSDescription}\n" 
                + $" {RuntimeInformation.ProcessArchitecture} {RuntimeInformation.FrameworkDescription}\n" 
                + $" Processor Count : {Environment.ProcessorCount} \n");
            
            ParseFlags(args);

            string category = GetCategory(args);
            string name = GetTestName(args);

            if (isDebug)
            {
                LogFactory.AddConsoleProvider();
            }
            else
            {
                ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Disabled;
            }

            Console.WriteLine($"\nNative version {Loop.NativeVersion}");
            Console.WriteLine($"Buffer leak detection : {nameof(ResourceLeakDetector)}.{ResourceLeakDetector.Level}\n");

            Run(category, name);

            if (!shouldPause)
            {
                return;
            }

            Console.WriteLine("Press any key to terminate the application");
            Console.ReadLine();
        }

        static string GetCategory(string[] args)
        {
            List<string> list = args?.Where(x => !x.StartsWith("-")).ToList();
            return list?.FirstOrDefault();
        }

        static string GetTestName(string[] args)
        {
            List<string> list = args?.Where(x => !x.StartsWith("-")).ToList();

            if (list == null
                || list.Count <= 1)
            {
                return null;
            }

            return list[1];
        }

        static void ParseFlags(string[] args)
        {
            List<string> switches = args?.Where(x => x.StartsWith("-")).ToList();
            isDebug = switches != null && switches.Any(x => string.Compare(x, Debug, StringComparison.OrdinalIgnoreCase) == 0);
            shouldPause = switches != null && switches.Any(x => string.Compare(x, Pause, StringComparison.OrdinalIgnoreCase) == 0);
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
                Console.WriteLine($"{nameof(RunLoopBenchmark)} {nameof(LoopCount)}");

                using (var loopCount = new LoopCount())
                {
                    loopCount.Run();
                }
            }
        }

        static void RunTimerBenchmark()
        {
            Console.WriteLine($"{nameof(RunTimerBenchmark)}");

            using (var timers = new MillionTimers())
            {
                timers.Run();
            }
        }

        static void RunAsyncBenchmark(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(MillionAsync)}");
                using (var millionAsync = new MillionAsync())
                {
                    millionAsync.Run();
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(AsyncHandles)} (1)");
                using (var asyncThreads = new AsyncHandles(1))
                {
                    asyncThreads.Run();
                }

                if (processorCount >= 2)
                {
                    Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(AsyncHandles)} (2)");
                    using (var asyncThreads = new AsyncHandles(2))
                    {
                        asyncThreads.Run();
                    }
                }

                if (processorCount >= 4)
                {
                    Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(AsyncHandles)} (4)");
                    using (var asyncThreads = new AsyncHandles(4))
                    {
                        asyncThreads.Run();
                    }
                }

                if (processorCount >= 8)
                {
                    Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(AsyncHandles)} (8)");
                    using (var asyncThreads = new AsyncHandles(8))
                    {
                        asyncThreads.Run();
                    }
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pummel", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(AsyncPummel)} (1)");
                using (var asyncPummel = new AsyncPummel(1))
                {
                    asyncPummel.Run();
                }

                if (processorCount >= 2)
                {
                    Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(AsyncPummel)} (2)");
                    using (var asyncPummel = new AsyncPummel(2))
                    {
                        asyncPummel.Run();
                    }
                }

                if (processorCount >= 4)
                {
                    Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(AsyncPummel)} (4)");
                    using (var asyncPummel = new AsyncPummel(4))
                    {
                        asyncPummel.Run();
                    }
                }

                if (processorCount >= 8)
                {
                    Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(AsyncPummel)} (8)");
                    using (var asyncPummel = new AsyncPummel(8))
                    {
                        asyncPummel.Run();
                    }
                }
            }
        }

        static void RunTcpBenchmark(string name)
        {
            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "writeBatch", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine($"{nameof(RunTcpBenchmark)} {nameof(TcpWriteBatch)}");
                using (var tcpWriteBatch = new TcpWriteBatch())
                {
                    tcpWriteBatch.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pingPong", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine($"{nameof(RunTcpBenchmark)} {nameof(TcpPingPong)}");
                using (var tcpPingPong = new TcpPingPong())
                {
                    tcpPingPong.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pump", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine($"{nameof(RunTcpBenchmark)} {nameof(Pump)} (1)");
                using (var tcpPump = new Pump(HandleType.Tcp, 1))
                {
                    tcpPump.Run();
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Console.WriteLine($"{nameof(RunTcpBenchmark)} {nameof(Pump)} (100)");
                    using (var tcpPump = new Pump(HandleType.Tcp, 100))
                    {
                        tcpPump.Run();
                    }
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pound", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine($"{nameof(RunTcpBenchmark)} {nameof(Pound)} (100)");
                using (var tcpPound = new Pound(HandleType.Tcp, 100))
                {
                    tcpPound.Run();
                }

                Console.WriteLine($"{nameof(RunTcpBenchmark)} {nameof(Pound)} (1000)");
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
                Console.WriteLine($"{nameof(RunPipeBenchmark)} {nameof(Pump)} (1)");
                using (var pipePump = new Pump(HandleType.Pipe, 1))
                {
                    pipePump.Run();
                }

                Console.WriteLine($"{nameof(RunPipeBenchmark)} {nameof(Pump)} (100)");
                using (var pipePump = new Pump(HandleType.Pipe, 100))
                {
                    pipePump.Run();
                }
            }

            if (string.IsNullOrEmpty(name)
                || string.Compare(name, "pound", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                Console.WriteLine($"{nameof(RunPipeBenchmark)} {nameof(Pound)} (100)");
                using (var tcpPound = new Pound(HandleType.Pipe, 100))
                {
                    tcpPound.Run();
                }

                Console.WriteLine($"{nameof(RunPipeBenchmark)} {nameof(Pound)} (1000)");
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
                var pairs = new[]
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
                    Console.WriteLine($"{nameof(RunUdpBenchmark)} Sender ({pair.Item1}) Receiver ({pair.Item2})");
                    using (var udpPummel = new UdpPummel(pair.Item1, pair.Item2, 0))
                    {
                        udpPummel.Run();
                    }
                }

                foreach (Tuple<int, int> pair in pairs)
                {
                    Console.WriteLine($"{nameof(RunUdpBenchmark)} Sender ({pair.Item1}) Receiver ({pair.Item2}) in 5 seconds.");
                    using (var udpPummel = new UdpPummel(pair.Item1, pair.Item2, 5000)) // ms
                    {
                        udpPummel.Run();
                    }
                }
            }
        }

        static void RunAddrInfoBenchmark()
        {
            Console.WriteLine($"{nameof(RunAddrInfoBenchmark)}");
            using (var requests = new GetAddrInfo())
            {
                requests.Run();
            }
        }
    }
}