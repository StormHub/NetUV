// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Runtime.CompilerServices;

    static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException(string messge) => throw new InvalidOperationException(messge);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRangeException(string paramName, string message) => throw new ArgumentOutOfRangeException(paramName, message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowIndexOutOfRangeException(string message) => throw new IndexOutOfRangeException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowIllegalReferenceCountException(int count) => throw new IllegalReferenceCountException(count);
    }
}
