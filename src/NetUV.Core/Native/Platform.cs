// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Native
{
    using System.Net.Sockets;
    using System.Runtime.InteropServices;

    static class Platform
    {
        static Platform()
        {
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            IsUnix = IsLinux || IsMacOS;

            OSSupportsIPv6 = Socket.OSSupportsIPv6;
            OSSupportsIPv4 = Socket.OSSupportsIPv4;
        }

        public static bool OSSupportsIPv6 { get; }

        public static bool OSSupportsIPv4 { get; }

        public static bool IsWindows { get; }

        public static bool IsUnix { get; }

        public static bool IsMacOS { get; }

        public static bool IsLinux { get; }
    }
}
