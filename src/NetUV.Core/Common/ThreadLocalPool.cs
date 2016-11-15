// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Forked from https://github.com/Azure/DotNetty
    /// </summary>
    public class ThreadLocalPool
    {
        public sealed class Handle
        {
            internal int LastRecycledId;
            internal int RecycleId;

            public object Value;
            internal Stack Stack;

            internal Handle(Stack stack)
            {
                this.Stack = stack;
            }

            public void Release<T>(T value)
                where T : class
            {
                Contract.Requires(value == this.Value, "value differs from one backed by this handle.");

                Stack stack = this.Stack;
                Thread thread = Thread.CurrentThread;
                if (stack.Thread == thread)
                {
                    stack.Push(this);
                    return;
                }

                ConditionalWeakTable<Stack, WeakOrderQueue> queueDictionary = DelayedPool.Value;
                WeakOrderQueue delayedRecycled;
                if (!queueDictionary.TryGetValue(stack, out delayedRecycled))
                {
                    var newQueue = new WeakOrderQueue(stack, thread);
                    delayedRecycled = newQueue;
                    queueDictionary.Add(stack, delayedRecycled);
                }
                delayedRecycled.Add(this);
            }
        }

        internal sealed class WeakOrderQueue
        {
            const int LinkCapacity = 16;

            sealed class Link
            {
                int writeIndex;

                internal readonly Handle[] Elements;
                internal Link Next;

                internal int ReadIndex { get; set; }

                internal int WriteIndex
                {
                    get { return Volatile.Read(ref this.writeIndex); }
                    set { Volatile.Write(ref this.writeIndex, value); }
                }

                internal Link()
                {
                    this.Elements = new Handle[LinkCapacity];
                }
            }

            Link head, tail;
            internal WeakOrderQueue Next;
            internal WeakReference<Thread> OwnerThread;
            readonly int id = Interlocked.Increment(ref idSource);

            internal bool IsEmpty => this.tail.ReadIndex == this.tail.WriteIndex;

            internal WeakOrderQueue(Stack stack, Thread thread)
            {
                Contract.Requires(stack != null);

                this.OwnerThread = new WeakReference<Thread>(thread);
                this.head = this.tail = new Link();
                lock (stack)
                {
                    this.Next = stack.HeadQueue;
                    stack.HeadQueue = this;
                }
            }

            internal void Add(Handle handle)
            {
                Contract.Requires(handle != null);

                handle.LastRecycledId = this.id;

                Link tailLink = this.tail;
                int writeIndex = tailLink.WriteIndex;
                if (writeIndex == LinkCapacity)
                {
                    this.tail = tailLink = tailLink.Next = new Link();
                    writeIndex = tailLink.WriteIndex;
                }
                tailLink.Elements[writeIndex] = handle;
                handle.Stack = null;
                tailLink.WriteIndex = writeIndex + 1;
            }

            internal bool Transfer(Stack dst)
            {
                // This method must be called by owner thread.
                Contract.Requires(dst != null);

                Link headLink = this.head;
                if (headLink == null)
                {
                    return false;
                }

                if (headLink.ReadIndex == LinkCapacity)
                {
                    if (headLink.Next == null)
                    {
                        return false;
                    }
                    this.head = headLink = headLink.Next;
                }

                int srcStart = headLink.ReadIndex;
                int srcEnd = headLink.WriteIndex;
                int srcSize = srcEnd - srcStart;
                if (srcSize == 0)
                {
                    return false;
                }

                int dstSize = dst.Size;
                int expectedCapacity = dstSize + srcSize;

                if (expectedCapacity > dst.Elements.Length)
                {
                    int actualCapacity = dst.IncreaseCapacity(expectedCapacity);
                    srcEnd = Math.Min(srcStart + actualCapacity - dstSize, srcEnd);
                }

                if (srcStart != srcEnd)
                {
                    Handle[] srcElems = headLink.Elements;
                    Handle[] dstElems = dst.Elements;
                    int newDstSize = dstSize;
                    for (int i = srcStart; i < srcEnd; i++)
                    {
                        Handle element = srcElems[i];
                        if (element.RecycleId == 0)
                        {
                            element.RecycleId = element.LastRecycledId;
                        }
                        else if (element.RecycleId != element.LastRecycledId)
                        {
                            throw new InvalidOperationException("recycled already");
                        }
                        element.Stack = dst;
                        dstElems[newDstSize++] = element;
                        srcElems[i] = null;
                    }
                    dst.Size = newDstSize;

                    if (srcEnd == LinkCapacity && headLink.Next != null)
                    {
                        this.head = headLink.Next;
                    }

                    headLink.ReadIndex = srcEnd;
                    return true;
                }
                else
                {
                    // The destination stack is full already.
                    return false;
                }
            }
        }

        internal sealed class Stack
        {
            internal readonly ThreadLocalPool Parent;
            internal readonly Thread Thread;

            internal Handle[] Elements;

            readonly int maxCapacity;
            //internal int size;

            WeakOrderQueue headQueue;
            WeakOrderQueue cursorQueue;
            WeakOrderQueue prevQueue;

            internal WeakOrderQueue HeadQueue
            {
                get { return Volatile.Read(ref this.headQueue); }
                set { Volatile.Write(ref this.headQueue, value); }
            }

            internal int Size { get; set; }

            internal Stack(int maxCapacity, ThreadLocalPool parent, Thread thread)
            {
                this.maxCapacity = maxCapacity;
                this.Parent = parent;
                this.Thread = thread;

                this.Elements = new Handle[Math.Min(InitialCapacity, maxCapacity)];
            }

            internal int IncreaseCapacity(int expectedCapacity)
            {
                int newCapacity = this.Elements.Length;
                int capacity = this.maxCapacity;
                do
                {
                    newCapacity <<= 1;
                }
                while (newCapacity < expectedCapacity && newCapacity < capacity);

                newCapacity = Math.Min(newCapacity, capacity);
                if (newCapacity != this.Elements.Length)
                {
                    Array.Resize(ref this.Elements, newCapacity);
                }

                return newCapacity;
            }

            internal void Push(Handle item)
            {
                Contract.Requires(item != null);
                if ((item.RecycleId | item.LastRecycledId) != 0)
                {
                    throw new InvalidOperationException("released already");
                }
                item.RecycleId = item.LastRecycledId = OwnThreadId;

                int size = this.Size;
                if (size >= this.maxCapacity)
                {
                    // Hit the maximum capacity - drop the possibly youngest object.
                    return;
                }
                if (size == this.Elements.Length)
                {
                    Array.Resize(ref this.Elements, Math.Min(size << 1, this.maxCapacity));
                }

                this.Elements[size] = item;
                this.Size = size + 1;
            }

            internal bool TryPop(out Handle item)
            {
                int size = this.Size;
                if (size == 0)
                {
                    if (!this.Scavenge())
                    {
                        item = null;
                        return false;
                    }
                    size = this.Size;
                }
                size--;
                Handle ret = this.Elements[size];
                if (ret.LastRecycledId != ret.RecycleId)
                {
                    throw new InvalidOperationException("recycled multiple times");
                }
                ret.RecycleId = 0;
                ret.LastRecycledId = 0;
                item = ret;
                this.Size = size;

                return true;
            }

            bool Scavenge()
            {
                // continue an existing scavenge, if any
                if (this.ScavengeSome())
                {
                    return true;
                }

                // reset our scavenge cursor
                this.prevQueue = null;
                this.cursorQueue = this.HeadQueue;
                return false;
            }

            bool ScavengeSome()
            {
                WeakOrderQueue cursor = this.cursorQueue;
                if (cursor == null)
                {
                    cursor = this.HeadQueue;
                    if (cursor == null)
                    {
                        return false;
                    }
                }

                bool success = false;
                WeakOrderQueue prev = this.prevQueue;
                do
                {
                    if (cursor.Transfer(this))
                    {
                        success = true;
                        break;
                    }

                    WeakOrderQueue next = cursor.Next;
                    Thread ownerThread;
                    if (!cursor.OwnerThread.TryGetTarget(out ownerThread))
                    {
                        // If the thread associated with the queue is gone, unlink it, after
                        // performing a volatile read to confirm there is no data left to collect.
                        // We never unlink the first queue, as we don't want to synchronize on updating the head.
                        if (!cursor.IsEmpty)
                        {
                            for (;;)
                            {
                                if (cursor.Transfer(this))
                                {
                                    success = true;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        if (prev != null)
                        {
                            prev.Next = next;
                        }
                    }
                    else
                    {
                        prev = cursor;
                    }

                    cursor = next;
                }
                while (cursor != null && !success);

                this.prevQueue = prev;
                this.cursorQueue = cursor;
                return success;
            }
        }

        internal static readonly int DefaultMaxCapacity = 262144;
        internal static readonly int InitialCapacity = Math.Min(256, DefaultMaxCapacity);
        static int idSource = int.MinValue;

        static readonly int OwnThreadId = Interlocked.Increment(ref idSource);

        internal static readonly DelayedThreadLocal DelayedPool = new DelayedThreadLocal();

        internal sealed class DelayedThreadLocal : FastThreadLocal<ConditionalWeakTable<Stack, WeakOrderQueue>>
        {
            protected override ConditionalWeakTable<Stack, WeakOrderQueue> GetInitialValue() => new ConditionalWeakTable<Stack, WeakOrderQueue>();
        }

        public ThreadLocalPool(int maxCapacity)
        {
            Contract.Requires(maxCapacity > 0);
            this.MaxCapacity = maxCapacity;
        }

        public int MaxCapacity { get; }
    }

    /// <summary>
    /// Forked from https://github.com/Azure/DotNetty
    /// </summary>
    public sealed class ThreadLocalPool<T> : ThreadLocalPool
        where T : class
    {
        readonly ThreadLocalStack threadLocal;
        readonly Func<Handle, T> valueFactory;
        readonly bool preCreate;

        public ThreadLocalPool(Func<Handle, T> valueFactory)
            : this(valueFactory, DefaultMaxCapacity)
        {
        }

        public ThreadLocalPool(Func<Handle, T> valueFactory, int maxCapacity)
            : this(valueFactory, maxCapacity, false)
        {
        }

        public ThreadLocalPool(Func<Handle, T> valueFactory, int maxCapacity, bool preCreate)
            : base(maxCapacity)
        {
            Contract.Requires(valueFactory != null);

            this.preCreate = preCreate;

            this.threadLocal = new ThreadLocalStack(this);
            this.valueFactory = valueFactory;
        }

        public T Take()
        {
            Stack stack = this.threadLocal.Value;
            Handle handle;
            if (!stack.TryPop(out handle))
            {
                handle = this.CreateValue(stack);
            }
            return (T)handle.Value;
        }

        Handle CreateValue(Stack stack)
        {
            var handle = new Handle(stack);
            T value = this.valueFactory(handle);
            handle.Value = value;
            return handle;
        }

        internal int ThreadLocalCapacity => this.threadLocal.Value.Elements.Length;

        internal int ThreadLocalSize => this.threadLocal.Value.Size;

        sealed class ThreadLocalStack : FastThreadLocal<Stack>
        {
            readonly ThreadLocalPool<T> owner;

            public ThreadLocalStack(ThreadLocalPool<T> owner)
            {
                this.owner = owner;
            }

            protected override Stack GetInitialValue()
            {
                var stack = new Stack(this.owner.MaxCapacity, this.owner, Thread.CurrentThread);
                if (this.owner.preCreate)
                {
                    for (int i = 0; i < this.owner.MaxCapacity; i++)
                    {
                        stack.Push(this.owner.CreateValue(stack));
                    }
                }
                return stack;
            }
        }
    }
}
