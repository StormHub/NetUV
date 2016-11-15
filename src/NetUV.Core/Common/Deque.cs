// Copyright (c) Johnny Z. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetUV.Core.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Forked from https://github.com/Azure/DotNetty
    /// A double-ended queue (deque), which provides O(1) indexed access, O(1) removals from the front and back, amortized
    /// O(1) insertions to the front and back, and O(N) insertions and removals anywhere else (with the operations getting
    /// slower as the index approaches the middle).
    /// </summary>
    sealed class Deque<T> : IList<T>, IList
    {
        const int DefaultCapacity = 8;

        // The circular buffer that holds the view.
        T[] buffer;

        // The offset into <see cref="buffer" /> where the view begins.
        int offset;

        public Deque(int capacity = DefaultCapacity)
        {
            Contract.Requires(capacity > 0);

            this.buffer = new T[capacity];
        }

        public Deque(IReadOnlyCollection<T> collection)
        {
            int count = collection?.Count ?? 0;
            if (count > 0)
            {
                this.buffer = new T[count];
                this.DoInsertRange(0, collection, count);
            }
            else
            {
                this.buffer = new T[DefaultCapacity];
            }
        }

        #region GenericListImplementations

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get
            {
                CheckExistingIndexArgument(this.Count, index);
                return this.DoGetItem(index);
            }

            set
            {
                CheckExistingIndexArgument(this.Count, index);
                this.DoSetItem(index, value);
            }
        }

        public void Insert(int index, T item)
        {
            CheckNewIndexArgument(this.Count, index);
            this.DoInsert(index, item);
        }

        public void RemoveAt(int index)
        {
            CheckExistingIndexArgument(this.Count, index);
            this.DoRemoveAt(index);
        }

        public int IndexOf(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            int ret = 0;
            foreach (T sourceItem in this)
            {
                if (comparer.Equals(item, sourceItem))
                    return ret;
                ++ret;
            }

            return -1;
        }

        void ICollection<T>.Add(T item) => this.DoInsert(this.Count, item);

        bool ICollection<T>.Contains(T item) => this.Contains(item, null);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), "Array is null");

            int count = this.Count;
            CheckRangeArguments(array.Length, arrayIndex, count);
            for (int i = 0; i != count; ++i)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public bool Remove(T item)
        {
            int index = this.IndexOf(item);
            if (index == -1)
                return false;

            this.DoRemoveAt(index);
            return true;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            int count = this.Count;
            for (int i = 0; i != count; ++i)
            {
                yield return this.DoGetItem(i);
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        #endregion

        #region ObjectListImplementations

        /// <summary>
        ///     Returns whether or not the type of a given item indicates it is appropriate for storing in this container.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <returns><c>true</c> if the item is appropriate to store in this container; otherwise, <c>false</c>.</returns>
        static bool ObjectIsT(object item)
        {
            if (item is T)
            {
                return true;
            }

            if (item == null)
            {
                TypeInfo typeInfo = typeof(T).GetTypeInfo();
                if (typeInfo.IsClass && !typeInfo.IsPointer)
                    return true; // classes, arrays, and delegates
                if (typeInfo.IsInterface)
                    return true; // interfaces
                if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return true; // nullable value types
            }

            return false;
        }

        int IList.Add(object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            this.AddToBack((T)value);
            return this.Count - 1;
        }

        bool IList.Contains(object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            return this.Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            return this.IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            this.Insert(index, (T)value);
        }

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => false;

        void IList.Remove(object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", nameof(value));
            this.Remove((T)value);
        }

        object IList.this[int index]
        {
            get { return this[index]; }

            set
            {
                if (!ObjectIsT(value))
                    throw new ArgumentException("Item is not of the correct type.", nameof(value));
                this[index] = (T)value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), "Destination array cannot be null.");
            CheckRangeArguments(array.Length, index, this.Count);

            for (int i = 0; i != this.Count; ++i)
            {
                try
                {
                    array.SetValue(this[i], index + i);
                }
                catch (InvalidCastException ex)
                {
                    throw new ArgumentException("Destination array is of incorrect type.", ex);
                }
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        #endregion

        #region GenericListHelpers

        /// <summary>
        /// Checks the <paramref name="index" /> argument to see if it refers to a valid insertion 
        /// point in a source of a given length.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="index">The index into the source.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index to an insertion point for the source. </exception>
        static void CheckNewIndexArgument(int sourceLength, int index)
        {
            if (index < 0 
                || index > sourceLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), 
                    $"Invalid new index {index} for source length {sourceLength}");
            }
        }

        /// <summary>
        /// Checks the <paramref name="index" /> argument to see if it refers to an existing element 
        /// in a source of a given length.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="index">The index into the source.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index" /> is not a valid index to an existing element for
        ///     the source.
        /// </exception>
        static void CheckExistingIndexArgument(int sourceLength, int index)
        {
            if (index < 0 
                || index >= sourceLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), 
                    $"Invalid existing index {index} for source length {sourceLength}");
            }
        }

        /// <summary>
        /// Checks the <paramref name="offset" /> and <paramref name="count" /> arguments for validity 
        /// when applied to a source  of a given length. Allows 0-element ranges, including a 0-element 
        /// range at the end of the source.
        /// </summary>
        /// <param name="sourceLength">The length of the source. This parameter is not checked for validity.</param>
        /// <param name="offset">The index into source at which the range begins.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <exception cref="ArgumentOutOfRangeException">Either <paramref name="offset" /> or <paramref name="count" /> is less than 0.</exception>
        /// <exception cref="ArgumentException">The range [offset, offset + count) is not within the range [0, sourceLength).</exception>
        static void CheckRangeArguments(int sourceLength, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), $"Invalid offset {offset}");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Invalid count {count}");
            }

            if (sourceLength - offset < count)
            {
                throw new ArgumentException(
                    $"Invalid offset ({offset}) or count + ({count}) for source length {sourceLength}");
            }
        }

        #endregion

        bool IsEmpty => this.Count == 0;

        bool IsFull => this.Count == this.Capacity;

        bool IsSplit => this.offset > (this.Capacity - this.Count);

        public int Capacity
        {
            get { return this.buffer.Length; }

            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity must be greater than 0.");

                if (value < this.Count)
                    throw new InvalidOperationException("Capacity cannot be set to a value less than Count");

                if (value == this.buffer.Length)
                    return;

                // Create the new buffer and copy our existing range.
                var newBuffer = new T[value];
                if (this.IsSplit)
                {
                    // The existing buffer is split, so we have to copy it in parts
                    int length = this.Capacity - this.offset;
                    Array.Copy(this.buffer, this.offset, newBuffer, 0, length);
                    Array.Copy(this.buffer, 0, newBuffer, length, this.Count - length);
                }
                else
                {
                    // The existing buffer is whole
                    Array.Copy(this.buffer, this.offset, newBuffer, 0, this.Count);
                }

                // Set up to use the new buffer.
                this.buffer = newBuffer;
                this.offset = 0;
            }
        }

        public int Count { get; private set; }

        int DequeIndexToBufferIndex(int index) => (index + this.offset) % this.Capacity;

        T DoGetItem(int index) => this.buffer[this.DequeIndexToBufferIndex(index)];

        void DoSetItem(int index, T item) => this.buffer[this.DequeIndexToBufferIndex(index)] = item;

        void DoInsert(int index, T item)
        {
            this.EnsureCapacityForOneElement();

            if (index == 0)
            {
                this.DoAddToFront(item);
                return;
            }
            else if (index == this.Count)
            {
                this.DoAddToBack(item);
                return;
            }

            this.DoInsertRange(index, new[] { item }, 1);
        }

        void DoRemoveAt(int index)
        {
            if (index == 0)
            {
                this.DoRemoveFromFront();
                return;
            }
            else if (index == this.Count - 1)
            {
                this.DoRemoveFromBack();
                return;
            }

            this.DoRemoveRange(index, 1);
        }

        int PostIncrement(int value)
        {
            int ret = this.offset;
            this.offset += value;
            this.offset %= this.Capacity;
            return ret;
        }

        int PreDecrement(int value)
        {
            this.offset -= value;
            if (this.offset < 0)
                this.offset += this.Capacity;
            return this.offset;
        }

        void DoAddToBack(T value)
        {
            this.buffer[this.DequeIndexToBufferIndex(this.Count)] = value;
            ++this.Count;
        }

        void DoAddToFront(T value)
        {
            this.buffer[this.PreDecrement(1)] = value;
            ++this.Count;
        }

        T DoRemoveFromBack()
        {
            T ret = this.buffer[this.DequeIndexToBufferIndex(this.Count - 1)];
            --this.Count;
            return ret;
        }

        T DoRemoveFromFront()
        {
            --this.Count;
            return this.buffer[this.PostIncrement(1)];
        }

        void DoInsertRange(int index, IEnumerable<T> collection, int collectionCount)
        {
            // Make room in the existing list
            if (index < this.Count / 2)
            {
                // Inserting into the first half of the list

                // Move lower items down: [0, index) -> [Capacity - collectionCount, Capacity - collectionCount + index)
                // This clears out the low "index" number of items, moving them "collectionCount" places down;
                //   after rotation, there will be a "collectionCount"-sized hole at "index".
                int copyCount = index;
                int writeIndex = this.Capacity - collectionCount;
                for (int j = 0; j != copyCount; ++j)
                    this.buffer[this.DequeIndexToBufferIndex(writeIndex + j)] = this.buffer[this.DequeIndexToBufferIndex(j)];

                // Rotate to the new view
                this.PreDecrement(collectionCount);
            }
            else
            {
                // Inserting into the second half of the list

                // Move higher items up: [index, count) -> [index + collectionCount, collectionCount + count)
                int copyCount = this.Count - index;
                int writeIndex = index + collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                    this.buffer[this.DequeIndexToBufferIndex(writeIndex + j)] = this.buffer[this.DequeIndexToBufferIndex(index + j)];
            }

            // Copy new items into place
            int i = index;
            foreach (T item in collection)
            {
                this.buffer[this.DequeIndexToBufferIndex(i)] = item;
                ++i;
            }

            // Adjust valid count
            this.Count += collectionCount;
        }

        void DoRemoveRange(int index, int collectionCount)
        {
            if (index == 0)
            {
                // Removing from the beginning: rotate to the new view
                this.PostIncrement(collectionCount);
                this.Count -= collectionCount;
                return;
            }
            else if (index == this.Count - collectionCount)
            {
                // Removing from the ending: trim the existing view
                this.Count -= collectionCount;
                return;
            }

            if ((index + (collectionCount / 2)) < this.Count / 2)
            {
                // Removing from first half of list

                // Move lower items up: [0, index) -> [collectionCount, collectionCount + index)
                int copyCount = index;
                int writeIndex = collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                    this.buffer[this.DequeIndexToBufferIndex(writeIndex + j)] = this.buffer[this.DequeIndexToBufferIndex(j)];

                // Rotate to new view
                this.PostIncrement(collectionCount);
            }
            else
            {
                // Removing from second half of list

                // Move higher items down: [index + collectionCount, count) -> [index, count - collectionCount)
                int copyCount = this.Count - collectionCount - index;
                int readIndex = index + collectionCount;
                for (int j = 0; j != copyCount; ++j)
                    this.buffer[this.DequeIndexToBufferIndex(index + j)] = this.buffer[this.DequeIndexToBufferIndex(readIndex + j)];
            }

            // Adjust valid count
            this.Count -= collectionCount;
        }

        void EnsureCapacityForOneElement()
        {
            if (this.IsFull)
            {
                this.Capacity = this.Capacity * 2;
            }
        }

        public void AddToBack(T value)
        {
            this.EnsureCapacityForOneElement();
            this.DoAddToBack(value);
        }

        public void AddToFront(T value)
        {
            this.EnsureCapacityForOneElement();
            this.DoAddToFront(value);
        }

        public void InsertRange(int index, IReadOnlyCollection<T> collection)
        {
            int collectionCount = collection.Count;
            CheckNewIndexArgument(this.Count, index);

            // Overflow-safe check for "this.Count + collectionCount > this.Capacity"
            if (collectionCount > this.Capacity - this.Count)
            {
                this.Capacity = checked(this.Count + collectionCount);
            }

            if (collectionCount == 0)
            {
                return;
            }

            this.DoInsertRange(index, collection, collectionCount);
        }

        public void RemoveRange(int from, int count)
        {
            CheckRangeArguments(this.Count, from, count);

            if (count == 0)
            {
                return;
            }

            this.DoRemoveRange(from, count);
        }

        public T RemoveFromBack()
        {
            if (this.IsEmpty)
                throw new InvalidOperationException("The deque is empty.");

            return this.DoRemoveFromBack();
        }

        public T RemoveFromFront()
        {
            if (this.IsEmpty)
                throw new InvalidOperationException("The deque is empty.");

            return this.DoRemoveFromFront();
        }

        public void Clear()
        {
            this.offset = 0;
            this.Count = 0;
        }
    }
}
