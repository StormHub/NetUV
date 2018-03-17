// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;

    static class ThrowHelper
    {
        public static void ThrowIndexOutOfRangeException(string message) => throw new IndexOutOfRangeException(message);

        public static void ThrowIllegalReferenceCountException(int count = 0) => throw new IllegalReferenceCountException(count);

        public static void ThrowIllegalReferenceCountException(int refCnt, int increment) => throw new IllegalReferenceCountException(refCnt, increment);

        public static void ThrowArgumentNullException(string message) => throw new ArgumentNullException(message);

        public static void ThrowArgumentOutOfRangeException(string name, string message) => throw new ArgumentOutOfRangeException(name, message);

        public static void ThrowObjectDisposedException(string message) => throw new ObjectDisposedException(nameof(message));

        public static void ThrowInvalidOperationException(string message) => throw new InvalidOperationException(nameof(message));

    }
}
