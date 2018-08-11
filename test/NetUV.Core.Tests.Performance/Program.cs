// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.CommandLineUtils;
    using NetUV.Core.Common;
    using NetUV.Core.Handles;
    using NetUV.Core.Logging;

    public class Program
    {
        const bool DefaultDebug = false;
        const bool DefaultPause = false;

        static readonly List<string> Benchmarks = new List<string>
        {
            "loop",
            "timer",
            "async", "asyncHandles", "asyncSend", "asyncPummel",
            "tcp", "tcpWriteBatch", "tcpPingPong", "tcpPump", "tcpPound",
            "pipe", "pipePump", "pipePound",
            "udp",
            "addrinfo"
        };

        static int processorCount;

        public static int Main(string[] args)
        {
            processorCount = Environment.ProcessorCount;
            var app = new CommandLineApplication(false)
            {
                Name = "Benchmark",
                FullName = "NetUV Benchmark",
                Description = "NetUV benchmarking tool"
            };
            app.HelpOption("-?|-h|--help");

            CommandOption debugOption = app.Option("-d|--debug",
                $"Enable debug output. Default is {DefaultDebug}.",
                CommandOptionType.SingleValue);

            CommandOption pauseOption = app.Option("-p|--pause",
                $"Pause after benchmark completed. Default is {DefaultPause}.",
                CommandOptionType.SingleValue);

            CommandOption categoryOption = app.Option("-c|--category",
                $"Category to run. Default is all [{string.Join(",", Benchmarks)}].",
                CommandOptionType.SingleValue);

            app.OnExecute(() =>
                {
                    try
                    {
                        bool debug = DefaultDebug;
                        if (debugOption.HasValue())
                        {
                            debug = bool.Parse(debugOption.Value());
                        }

                        bool pause = DefaultPause;
                        if (pauseOption.HasValue())
                        {
                            pause = bool.Parse(pauseOption.Value());
                        }

                        var categoryList = new List<string>();
                        if (categoryOption.HasValue())
                        {
                            string value = categoryOption.Value();
                            if (Benchmarks.Contains(value, StringComparer.OrdinalIgnoreCase))
                            {
                                categoryList.Add(value);
                            }
                        }
                        if (categoryList.Count == 0)
                        {
                            categoryList.AddRange(Benchmarks);
                        }

                        return Run(categoryList, debug, pause);
                    }
                    catch (Exception e)
                    {
                        app.ShowHelp();
                        Console.WriteLine(e);
                        return 2;
                    }
                });

            return app.Execute(args);
        }

        static int Run(IReadOnlyCollection<string> list, bool debug, bool pause)
        {
            Console.WriteLine(
                  $"\n{RuntimeInformation.OSArchitecture} {RuntimeInformation.OSDescription}"
                + $"\n{RuntimeInformation.ProcessArchitecture} {RuntimeInformation.FrameworkDescription}"
                + $"\nProcessor Count : {processorCount}\n");

            if (debug)
            {
                LogFactory.AddConsoleProvider();
            }
            else
            {
                ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Disabled;
            }

            Console.WriteLine($"\nLibuv Native version {Loop.NativeVersion}");
            Console.WriteLine($"Buffer leak detection : {nameof(ResourceLeakDetector)}.{ResourceLeakDetector.Level}\n");

            if (list.Contains("loop", StringComparer.OrdinalIgnoreCase))
            {
                RunLoopBenchmark();
            }
            if (list.Contains("timer", StringComparer.OrdinalIgnoreCase))
            {
                RunTimerBenchmark();
            }

            List<string> names = list.Contains("async", StringComparer.OrdinalIgnoreCase) 
                ? new List<string> { "asyncHandles", "asyncSend", "asyncPummel" }
                : list.Where(x => x.StartsWith("async")).ToList();
            if (names.Count > 0)
            {
                RunAsyncBenchmark(names);
            }

            names = list.Contains("tcp", StringComparer.OrdinalIgnoreCase)
                ? new List<string> { "tcpWriteBatch", "tcpPingPong", "tcpPump", "tcpPound" }
                : list.Where(x => x.StartsWith("tcp")).ToList();
            if (names.Count > 0)
            {
                RunTcpBenchmark(names);
            }

            names = list.Contains("pipe", StringComparer.OrdinalIgnoreCase)
                ? new List<string> { "pipePump", "pipePound" }
                : list.Where(x => x.StartsWith("pipe")).ToList();
            if (names.Count > 0)
            {
                RunPipeBenchmark(names);
            }

            if (list.Contains("udp", StringComparer.OrdinalIgnoreCase))
            {
                RunUdpBenchmark();
            }

            if (list.Contains("addrinfo", StringComparer.OrdinalIgnoreCase))
            {
                RunAddrInfoBenchmark();
            }

            if (pause)
            {
                Console.WriteLine("Press any key to terminate the application");
                Console.ReadLine();
            }

            return 0;
        }

        static void RunLoopBenchmark()
        {
            Console.WriteLine($"{nameof(RunLoopBenchmark)} {nameof(LoopCount)}");
            using (var loopCount = new LoopCount())
            {
                loopCount.Run();
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

        static void RunAsyncBenchmark(IReadOnlyCollection<string> list)
        {
            if (list.Contains("asyncHandles", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{nameof(RunAsyncBenchmark)} {nameof(MillionAsync)}");
                using (var millionAsync = new MillionAsync())
                {
                    millionAsync.Run();
                }
            }

            if (list.Contains("asyncSend", StringComparer.OrdinalIgnoreCase))
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

            if (list.Contains("asyncPummel", StringComparer.OrdinalIgnoreCase))
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

        static void RunTcpBenchmark(IReadOnlyCollection<string> list)
        {
            if (list.Contains("tcpWriteBatch", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{nameof(RunTcpBenchmark)} {nameof(TcpWriteBatch)}");
                using (var tcpWriteBatch = new TcpWriteBatch())
                {
                    tcpWriteBatch.Run();
                }
            }

            if (list.Contains("tcpPingPong", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{nameof(RunTcpBenchmark)} {nameof(TcpPingPong)}");
                using (var tcpPingPong = new TcpPingPong())
                {
                    tcpPingPong.Run();
                }
            }

            if (list.Contains("tcpPump", StringComparer.OrdinalIgnoreCase))
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

            if (list.Contains("tcpPound", StringComparer.OrdinalIgnoreCase))
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

        static void RunPipeBenchmark(IReadOnlyCollection<string> list)
        {
            if (list.Contains("pipePump", StringComparer.OrdinalIgnoreCase))
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

            if (list.Contains("pipePound", StringComparer.OrdinalIgnoreCase))
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

        static void RunUdpBenchmark()
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