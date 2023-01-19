// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    abstract class FastThreadLocal
    {
        static readonly int VariablesToRemoveIndex = InternalThreadLocalMap.NextVariableIndex();

        public static void RemoveAll()
        {
            InternalThreadLocalMap threadLocalMap = InternalThreadLocalMap.GetIfSet();
            if (threadLocalMap == null)
            {
                return;
            }

            try
            {
                object v = threadLocalMap.GetIndexedVariable(VariablesToRemoveIndex);
                if (v != null && v != InternalThreadLocalMap.Unset)
                {
                    var variablesToRemove = (HashSet<FastThreadLocal>)v;
                    foreach (FastThreadLocal tlv in variablesToRemove) // todo: do we need to make a snapshot?
                    {
                        tlv.Remove(threadLocalMap);
                    }
                }
            }
            finally
            {
                InternalThreadLocalMap.Remove();
            }
        }

        public static void Destroy() => InternalThreadLocalMap.Destroy();

        protected static void AddToVariablesToRemove(InternalThreadLocalMap threadLocalMap, FastThreadLocal variable)
        {
            object v = threadLocalMap.GetIndexedVariable(VariablesToRemoveIndex);
            HashSet<FastThreadLocal> variablesToRemove;
            if (v == InternalThreadLocalMap.Unset || v == null)
            {
                variablesToRemove = new HashSet<FastThreadLocal>(); // Collections.newSetFromMap(new IdentityHashMap<FastThreadLocal<?>, Boolean>());
                threadLocalMap.SetIndexedVariable(VariablesToRemoveIndex, variablesToRemove);
            }
            else
            {
                variablesToRemove = (HashSet<FastThreadLocal>)v;
            }

            variablesToRemove.Add(variable);
        }

        protected static void RemoveFromVariablesToRemove(InternalThreadLocalMap threadLocalMap, FastThreadLocal variable)
        {
            object v = threadLocalMap.GetIndexedVariable(VariablesToRemoveIndex);

            if (v == InternalThreadLocalMap.Unset || v == null)
            {
                return;
            }

            var variablesToRemove = (HashSet<FastThreadLocal>)v;
            variablesToRemove.Remove(variable);
        }

        public abstract void Remove(InternalThreadLocalMap threadLocalMap);
    }

    class FastThreadLocal<T> : FastThreadLocal
        where T : class
    {
        readonly int index;

        public static int Count => InternalThreadLocalMap.GetIfSet()?.Count ?? 0;

        public FastThreadLocal()
        {
            this.index = InternalThreadLocalMap.NextVariableIndex();
        }

        public T Value
        {
            get => this.Get(InternalThreadLocalMap.Get());
            set => this.Set(InternalThreadLocalMap.Get(), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(InternalThreadLocalMap threadLocalMap)
        {
            object v = threadLocalMap.GetIndexedVariable(this.index);
            if (v != InternalThreadLocalMap.Unset)
            {
                return (T)v;
            }

            return this.Initialize(threadLocalMap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T Initialize(InternalThreadLocalMap threadLocalMap)
        {
            T v = this.GetInitialValue();

            threadLocalMap.SetIndexedVariable(this.index, v);
            AddToVariablesToRemove(threadLocalMap, this);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(InternalThreadLocalMap threadLocalMap, T value)
        {
            if (threadLocalMap.SetIndexedVariable(this.index, value))
            {
                AddToVariablesToRemove(threadLocalMap, this);
            }
        }

        public bool IsSet() => this.IsSet(InternalThreadLocalMap.GetIfSet());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(InternalThreadLocalMap threadLocalMap) => threadLocalMap != null && threadLocalMap.IsIndexedVariableSet(this.index);

        protected virtual T GetInitialValue() => null;

        public void Remove() => this.Remove(InternalThreadLocalMap.GetIfSet());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override void Remove(InternalThreadLocalMap threadLocalMap)
        {
            if (threadLocalMap == null)
            {
                return;
            }

            object v = threadLocalMap.RemoveIndexedVariable(this.index);
            RemoveFromVariablesToRemove(threadLocalMap, this);

            if (v != InternalThreadLocalMap.Unset)
            {
                this.OnRemoval((T)v);
            }
        }

        protected virtual void OnRemoval(T value)
        {
        }
    }
}
