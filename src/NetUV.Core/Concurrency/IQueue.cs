// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Concurrency
{
    interface IQueue<T>
    {
        bool TryEnqueue(T item);

        bool TryDequeue(out T item);

        bool TryPeek(out T item);

        int Count { get; }

        bool IsEmpty { get; }

        void Clear();
    }

    abstract class AbstractQueue<T> : IQueue<T>
    {
        public abstract bool TryEnqueue(T item);

        public abstract bool TryDequeue(out T item);

        public abstract bool TryPeek(out T item);

        public abstract int Count { get; }

        public abstract bool IsEmpty { get; }

        public abstract void Clear();
    }

}
