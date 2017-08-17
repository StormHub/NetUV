// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using NetUV.Core.Concurrency;
    using NetUV.Core.Native;

    static class PlatformDependent
    {
        static int seed = (int)(Stopwatch.GetTimestamp() & 0xFFFFFFFF); //used to safly cast long to int, because the timestamp returned is long and it doesn't fit into an int
        static readonly ThreadLocal<Random> ThreadLocalRandom = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed))); //used to simulate java ThreadLocalRandom

        public static Random GetThreadLocalRandom() => ThreadLocalRandom.Value;

        public static IQueue<T> NewFixedMpscQueue<T>(int capacity) where T : class => new MpscArrayQueue<T>(capacity);

        public static IQueue<T> NewMpscQueue<T>() where T : class => new CompatibleConcurrentQueue<T>();

        public static unsafe void CopyMemory(byte[] src, int srcIndex, byte[] dst, int dstIndex, int length)
        {
            if (length == 0)
            {
                return;
            }

            if (ReferenceEquals(src, dst))
            {
                // On linux, it seems like Unsafe.CopyBlock does not work properly
                // if the source and destination array are the same and with overlapped
                // regions, have to use Buffer.MemoryCopy.
                // For example: 
                // for array of 1024 bytes, copy bytes from 1 to 1024 into 0 to 1023
                if (Platform.IsLinux)
                {
                    fixed (byte* bytes = src)
                        Buffer.MemoryCopy(bytes + srcIndex, bytes + dstIndex, (uint)length, (uint)length);
                }
                else
                {
                    fixed (byte* bytes = src)
                        Unsafe.CopyBlock(bytes + dstIndex, bytes + srcIndex, (uint)length);
                }
            }
            else
            {
                fixed (byte* source = &src[srcIndex])
                    fixed (byte* destination = &dst[dstIndex])
                        Unsafe.CopyBlock(destination, source, (uint)length);
            }
        }

        public static unsafe void Clear(byte[] src, int srcIndex, int length)
        {
            if (length == 0)
            {
                return;
            }

            fixed (void* source = &src[srcIndex])
                Unsafe.InitBlock(source, default(byte), (uint)length);
        }
    }
}
