// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Concurrency
{
    using System.Collections.Concurrent;

    public sealed class CompatibleConcurrentQueue<T> : ConcurrentQueue<T>, IQueue<T>
    {
        public bool TryEnqueue(T element)
        {
            this.Enqueue(element);
            return true;
        }

        void IQueue<T>.Clear()
        {
            while (this.TryDequeue(out T _))
            {
            }
        }
    }
}
