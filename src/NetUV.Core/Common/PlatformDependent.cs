// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace NetUV.Core.Common
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using NetUV.Core.Concurrency;
    using NetUV.Core.Logging;
    using NetUV.Core.Native;

    static class PlatformDependent
    {
        static readonly ILog Logger = LogFactory.ForContext(typeof(PlatformDependent));

        static readonly bool UseDirectBuffer;
        static readonly bool IsLinux = Platform.IsLinux;

        static PlatformDependent()
        {
            UseDirectBuffer = !SystemPropertyUtil.GetBoolean("io.noPreferDirect", false);
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("io.noPreferDirect: {0}", !UseDirectBuffer);
            }
        }

        public static bool DirectBufferPreferred => UseDirectBuffer;

        static int seed = (int)(Stopwatch.GetTimestamp() & 0xFFFFFFFF); //used to safly cast long to int, because the timestamp returned is long and it doesn't fit into an int
        static readonly ThreadLocal<Random> ThreadLocalRandom = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed))); //used to simulate java ThreadLocalRandom

        public static Random GetThreadLocalRandom() => ThreadLocalRandom.Value;

        public static IQueue<T> NewFixedMpscQueue<T>(int capacity) where T : class => new MpscArrayQueue<T>(capacity);

        public static IQueue<T> NewMpscQueue<T>() where T : class => new CompatibleConcurrentQueue<T>();

        public static unsafe void CopyMemory(byte[] src, int srcIndex, byte[] dst, int dstIndex, int length)
        {
            if (length > 0)
            {
                fixed (byte* source = &src[srcIndex])
                    fixed (byte* destination = &dst[dstIndex])
                        if (IsLinux)
                        {
                            Buffer.MemoryCopy(source, destination, length, length);
                        }
                        else
                        {
                            Unsafe.CopyBlockUnaligned(destination, source, unchecked((uint)length));
                        }
            }
        }

        public static unsafe void Clear(byte[] src, int srcIndex, int length)
        {
            if (length > 0)
            {
                fixed (void* source = &src[srcIndex])
                    Unsafe.InitBlockUnaligned(source, default(byte), unchecked((uint)length));
            }
        }

        public static unsafe void CopyMemory(byte* src, byte* dst, int length)
        {
            if (length > 0)
            {
                if (IsLinux)
                {
                    Buffer.MemoryCopy(src, dst, length, length);
                }
                else
                {
                    Unsafe.CopyBlockUnaligned(dst, src, unchecked((uint)length));
                }
            }
        }

        public static unsafe void CopyMemory(byte* src, byte[] dst, int dstIndex, int length)
        {
            if (length > 0)
            {
                fixed (byte* destination = &dst[dstIndex])
                    if (IsLinux)
                    {
                        Buffer.MemoryCopy(src, destination, length, length);
                    }
                    else
                    {
                        Unsafe.CopyBlockUnaligned(destination, src, unchecked((uint)length));
                    }
            }
        }

        public static unsafe void CopyMemory(byte[] src, int srcIndex, byte* dst, int length)
        {
            if (length > 0)
            {
                fixed (byte* source = &src[srcIndex])
                    if (IsLinux)
                    {
                        Buffer.MemoryCopy(source, dst, length, length);
                    }
                    else
                    {
                        Unsafe.CopyBlockUnaligned(dst, source, unchecked((uint)length));
                    }
            }
        }

        public static unsafe void SetMemory(byte* src, int length, byte value)
        {
            if (length > 0)
            {
                Unsafe.InitBlockUnaligned(src, value, unchecked((uint)length));
            }
        }

        public static unsafe void SetMemory(byte[] src, int srcIndex, int length, byte value)
        {
            if (length > 0)
            {
                fixed (byte* source = &src[srcIndex])
                    Unsafe.InitBlockUnaligned(source, value, unchecked((uint)length));
            }
        }
    }
}
