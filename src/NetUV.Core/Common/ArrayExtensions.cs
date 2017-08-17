// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System.Diagnostics.Contracts;

    static class ArrayExtensions
    {
        public static readonly byte[] ZeroBytes = new byte[0];

        public static void Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        public static void Fill<T>(this T[] array, int offset, int count, T value)
        {
            Contract.Requires(count + offset <= array.Length);

            for (int i = offset; i < count + offset; i++)
            {
                array[i] = value;
            }
        }
    }
}
