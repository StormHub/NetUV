// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Buffers
{
    using System;
    using System.Diagnostics.Contracts;

    public abstract class ByteProcessor
    {
        public sealed class IndexOfProcessor : ByteProcessor
        {
            readonly byte byteToFind;

            public IndexOfProcessor(byte byteToFind)
            {
                this.byteToFind = byteToFind;
            }

            public override bool Process(byte value) => value != this.byteToFind;
        }

        public sealed class PredicateProcessor : ByteProcessor
        {
            readonly Func<byte, bool> predicate;

            public PredicateProcessor(Func<byte, bool> predicate)
            {
                Contract.Assert(predicate != null);

                this.predicate = predicate;
            }

            public override bool Process(byte value) => this.predicate(value);
        }

        public abstract bool Process(byte value);
    }
}
