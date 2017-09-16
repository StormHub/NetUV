﻿// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;

    sealed class InternalThreadLocalMap
    {
        public static readonly object Unset = new object();

        [ThreadStatic]
        static InternalThreadLocalMap slowThreadLocalMap;

        static int nextIndex;

        /// Used by {@link FastThreadLocal}
        object[] indexedVariables;

        // Core thread-locals
        int futureListenerStackDepth;
        int localChannelReaderStackDepth;

        // String-related thread-locals
        StringBuilder stringBuilder;

        internal static int NextVariableIndex()
        {
            int index = Interlocked.Increment(ref nextIndex);
            if (index < 0)
            {
                Interlocked.Decrement(ref nextIndex);
                throw new InvalidOperationException("too many thread-local indexed variables");
            }
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InternalThreadLocalMap GetIfSet() => slowThreadLocalMap;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InternalThreadLocalMap Get()
        {
            InternalThreadLocalMap ret = slowThreadLocalMap;
            if (ret == null)
            {
                ret = new InternalThreadLocalMap();
                slowThreadLocalMap = ret;
            }
            return ret;
        }

        public static void Remove() => slowThreadLocalMap = null;

        public static void Destroy() => slowThreadLocalMap = null;

        // Cache line padding (must be public)
        // With CompressedOops enabled, an instance of this class should occupy at least 128 bytes.
        // ReSharper disable InconsistentNaming
        public long rp1, rp2, rp3, rp4, rp5, rp6, rp7, rp8, rp9;

        InternalThreadLocalMap()
        {
            this.indexedVariables = CreateIndexedVariableTable();
        }

        static object[] CreateIndexedVariableTable()
        {
            var array = new object[32];

            array.Fill(Unset);
            return array;
        }

        public int Count
        {
            get
            {
                int count = 0;

                if (this.futureListenerStackDepth != 0)
                {
                    count++;
                }
                if (this.localChannelReaderStackDepth != 0)
                {
                    count++;
                }
                if (this.stringBuilder != null)
                {
                    count++;
                }
                foreach (object o in this.indexedVariables)
                {
                    if (o != Unset)
                    {
                        count++;
                    }
                }

                // We should subtract 1 from the count because the first element in 'indexedVariables' is reserved
                // by 'FastThreadLocal' to keep the list of 'FastThreadLocal's to remove on 'FastThreadLocal.RemoveAll()'.
                return count - 1;
            }
        }

        public StringBuilder StringBuilder
        {
            get
            {
                StringBuilder builder = this.stringBuilder;
                if (builder == null)
                {
                    this.stringBuilder = builder = new StringBuilder(512);
                }
                else
                {
                    builder.Length = 0;
                }
                return builder;
            }
        }

        public int FutureListenerStackDepth
        {
            get => this.futureListenerStackDepth;
            set => this.futureListenerStackDepth = value;
        }

        public int LocalChannelReaderStackDepth
        {
            get => this.localChannelReaderStackDepth;
            set => this.localChannelReaderStackDepth = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetIndexedVariable(int index)
        {
            object[] lookup = this.indexedVariables;
            return index < lookup.Length ? lookup[index] : Unset;
        }

        public bool SetIndexedVariable(int index, object value)
        {
            object[] lookup = this.indexedVariables;
            if (index < lookup.Length)
            {
                object oldValue = lookup[index];
                lookup[index] = value;
                return oldValue == Unset;
            }
            else
            {
                this.ExpandIndexedVariableTableAndSet(index, value);
                return true;
            }
        }

        void ExpandIndexedVariableTableAndSet(int index, object value)
        {
            object[] oldArray = this.indexedVariables;
            int oldCapacity = oldArray.Length;
            int newCapacity = index;
            newCapacity |= newCapacity.RightUShift(1);
            newCapacity |= newCapacity.RightUShift(2);
            newCapacity |= newCapacity.RightUShift(4);
            newCapacity |= newCapacity.RightUShift(8);
            newCapacity |= newCapacity.RightUShift(16);
            newCapacity++;

            var newArray = new object[newCapacity];
            oldArray.CopyTo(newArray, 0);
            newArray.Fill(oldCapacity, newArray.Length - oldCapacity, Unset);
            newArray[index] = value;
            this.indexedVariables = newArray;
        }

        public object RemoveIndexedVariable(int index)
        {
            object[] lookup = this.indexedVariables;
            if (index < lookup.Length)
            {
                object v = lookup[index];
                lookup[index] = Unset;
                return v;
            }
            else
            {
                return Unset;
            }
        }

        public bool IsIndexedVariableSet(int index)
        {
            object[] lookup = this.indexedVariables;
            return index < lookup.Length && lookup[index] != Unset;
        }
    }
}
