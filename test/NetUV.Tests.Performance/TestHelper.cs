// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Tests.Performance
{
    using System.Net;
    using System.Runtime.InteropServices;

    enum HandleType
    {
        Tcp,
        Pipe,
        Udp
    }

    static class TestHelper
    {
        const int Port = 9889;
        internal const long NanoSeconds = 1000000000;

        static TestHelper()
        {
            LoopbackEndPoint = new IPEndPoint(IPAddress.Loopback, Port);

            LocalPipeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "\\\\?\\pipe\\test"
                : "/tmp/uv-test-sock";

            AnyEndPoint = new IPEndPoint(IPAddress.Any, Port);
        }

        internal static string Format(int value) => value.ToString("#,##0");

        internal static string Format(long value) => value.ToString("#,##0");

        internal static string Format(double value) => value.ToString("#,##0.00");

        internal static IPEndPoint AnyEndPoint { get; }

        internal static IPEndPoint LoopbackEndPoint { get; }

        internal static string LocalPipeName { get; }
    }
}
