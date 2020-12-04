/*
 * MIT License
 *
 * Copyright (c) 2018 Clark Yang
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of 
 * this software and associated documentation files (the "Software"), to deal in 
 * the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies 
 * of the Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all 
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
 * SOFTWARE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using INotifyCollectionChanged = System.Collections.Specialized.INotifyCollectionChanged;
using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;
using NotifyCollectionChangedEventArgs = System.Collections.Specialized.NotifyCollectionChangedEventArgs;
using NotifyCollectionChangedEventHandler = System.Collections.Specialized.NotifyCollectionChangedEventHandler;

namespace Loxodon.Framework.Observables
{
    [Serializable]
    public class ObservableList<T> : IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs CountEventArgs = new PropertyChangedEventArgs("Count");
        private static readonly PropertyChangedEventArgs IndexerEventArgs = new PropertyChangedEventArgs("Item[]");
        private readonly object collectionChangedLock = new object();

        private readonly object propertyChangedLock = new object();
        private NotifyCollectionChangedEventHandler collectionChanged;

        private SimpleMonitor monitor = new SimpleMonitor();
        private PropertyChangedEventHandler propertyChanged;

        [NonSerialized] private object syncRoot;

        public ObservableList()
        {
            Items = new List<T>();
        }

        public ObservableList(List<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            Items = new List<T>();
            using (IEnumerator<T> enumerator = list.GetEnumerator())
            {
                while (enumerator.MoveNext()) Items.Add(enumerator.Current);
            }
        }

        protected IList<T> Items { get; }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                {
                    ICollection c = Items as ICollection;
                    if (c != null)
                        syncRoot = c.SyncRoot;
                    else
                        Interlocked.CompareExchange<object>(ref syncRoot, new object(), null);
                }

                return syncRoot;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (array.Rank != 1)
                throw new ArgumentException("RankMultiDimNotSupported");

            if (array.GetLowerBound(0) != 0)
                throw new ArgumentException("NonZeroLowerBound");

            if (index < 0)
                throw new ArgumentOutOfRangeException(string.Format("ArgumentOutOfRangeException:{0}", index));

            if (array.Length - index < Count)
                throw new ArgumentException("ArrayPlusOffTooSmall");

            var tArray = array as T[];
            if (tArray != null)
            {
                Items.CopyTo(tArray, index);
            }
            else
            {
                Type targetType = array.GetType().GetElementType();
                Type sourceType = typeof(T);
                if (!(targetType.IsAssignableFrom(sourceType) || sourceType.IsAssignableFrom(targetType)))
                    throw new ArgumentException("InvalidArrayType");

                var objects = array as object[];
                if (objects == null)
                    throw new ArgumentException("InvalidArrayType");

                var count = Items.Count;
                try
                {
                    for (var i = 0; i < count; i++) objects[index++] = Items[i];
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException("InvalidArrayType");
                }
            }
        }

        object IList.this[int index]
        {
            get => Items[index];
            set
            {
                if (value == null && !(default(T) == null))
                    throw new ArgumentNullException("value");

                try
                {
                    this[index] = (T) value;
                }
                catch (InvalidCastException e)
                {
                    throw new ArgumentException("", e);
                }
            }
        }

        bool IList.IsReadOnly => Items.IsReadOnly;

        bool IList.IsFixedSize
        {
            get
            {
                IList list = Items as IList;
                if (list != null) return list.IsFixedSize;
                return Items.IsReadOnly;
            }
        }

        int IList.Add(object value)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            if (value == null && !(default(T) == null))
                throw new ArgumentNullException("value");

            try
            {
                Add((T) value);
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException("", e);
            }

            return Count - 1;
        }

        bool IList.Contains(object value)
        {
            if (IsCompatibleObject(value)) return Contains((T) value);
            return false;
        }

        int IList.IndexOf(object value)
        {
            if (IsCompatibleObject(value)) return IndexOf((T) value);
            return -1;
        }

        void IList.Insert(int index, object value)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            if (value == null && !(default(T) == null))
                throw new ArgumentNullException("value");

            try
            {
                Insert(index, (T) value);
            }
            catch (InvalidCastException e)
            {
                throw new ArgumentException("", e);
            }
        }

        void IList.Remove(object value)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            if (IsCompatibleObject(value)) Remove((T) value);
        }

        public int Count => Items.Count;

        public T this[int index]
        {
            get => Items[index];
            set
            {
                if (Items.IsReadOnly)
                    throw new NotSupportedException("ReadOnlyCollection");

                if (index < 0 || index >= Items.Count)
                    throw new ArgumentOutOfRangeException(string.Format("ArgumentOutOfRangeException:{0}", index));

                SetItem(index, value);
            }
        }

        public void Add(T item)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            var index = Items.Count;
            InsertItem(index, item);
        }

        public void Clear()
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            ClearItems();
        }

        public void CopyTo(T[] array, int index)
        {
            Items.CopyTo(array, index);
        }

        public bool Contains(T item)
        {
            return Items.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return Items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            if (index < 0 || index > Items.Count)
                throw new ArgumentOutOfRangeException(string.Format("ArgumentOutOfRangeException:{0}", index));

            InsertItem(index, item);
        }

        public bool Remove(T item)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            var index = Items.IndexOf(item);
            if (index < 0)
                return false;
            RemoveItem(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            if (index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException(string.Format("ArgumentOutOfRangeException:{0}", index));

            RemoveItem(index);
        }

        bool ICollection<T>.IsReadOnly => Items.IsReadOnly;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) Items).GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                lock (collectionChangedLock)
                {
                    collectionChanged += value;
                }
            }
            remove
            {
                lock (collectionChangedLock)
                {
                    collectionChanged -= value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                lock (propertyChangedLock)
                {
                    propertyChanged += value;
                }
            }
            remove
            {
                lock (propertyChangedLock)
                {
                    propertyChanged -= value;
                }
            }
        }

        public void Move(int oldIndex, int newIndex)
        {
            MoveItem(oldIndex, newIndex);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            var index = Items.Count;
            InsertItem(index, collection);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            if (index < 0 || index > Items.Count)
                throw new ArgumentOutOfRangeException(string.Format("ArgumentOutOfRangeException:{0}", index));

            InsertItem(index, collection);
        }

        public void RemoveRange(int index, int count)
        {
            if (Items.IsReadOnly)
                throw new NotSupportedException("ReadOnlyCollection");

            if (index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException(string.Format("ArgumentOutOfRangeException:{0}", index));

            RemoveItem(index, count);
        }

        protected virtual void ClearItems()
        {
            CheckReentrancy();
            Items.Clear();
            OnPropertyChanged(CountEventArgs);
            OnPropertyChanged(IndexerEventArgs);
            OnCollectionReset();
        }

        protected virtual void RemoveItem(int index)
        {
            CheckReentrancy();
            T removedItem = this[index];

            Items.RemoveAt(index);

            OnPropertyChanged(CountEventArgs);
            OnPropertyChanged(IndexerEventArgs);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
        }

        protected virtual void RemoveItem(int index, int count)
        {
            CheckReentrancy();

            var list = Items as List<T>;
            var changedItems = list.GetRange(index, count);
            list.RemoveRange(index, count);

            OnPropertyChanged(CountEventArgs);
            OnPropertyChanged(IndexerEventArgs);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, changedItems, index);
        }

        protected virtual void InsertItem(int index, T item)
        {
            CheckReentrancy();

            Items.Insert(index, item);

            OnPropertyChanged(CountEventArgs);
            OnPropertyChanged(IndexerEventArgs);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        protected virtual void InsertItem(int index, IEnumerable<T> collection)
        {
            CheckReentrancy();

            (Items as List<T>).InsertRange(index, collection);

            OnPropertyChanged(CountEventArgs);
            OnPropertyChanged(IndexerEventArgs);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, ToList(collection), index);
        }

        protected virtual void SetItem(int index, T item)
        {
            CheckReentrancy();
            T originalItem = this[index];

            Items[index] = item;

            OnPropertyChanged(IndexerEventArgs);
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, originalItem, item,
                index);
        }

        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            CheckReentrancy();

            T removedItem = this[oldIndex];

            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, removedItem);

            OnPropertyChanged(IndexerEventArgs);
            OnCollectionChanged(NotifyCollectionChangedAction.Move, removedItem, newIndex,
                oldIndex);
        }


        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (propertyChanged != null) propertyChanged(this, e);
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (collectionChanged != null)
                using (BlockReentrancy())
                {
                    collectionChanged(this, e);
                }
        }

        protected IDisposable BlockReentrancy()
        {
            monitor.Enter();
            return monitor;
        }

        protected void CheckReentrancy()
        {
            if (monitor.Busy)
                if (collectionChanged != null && collectionChanged.GetInvocationList().Length > 1)
                    throw new InvalidOperationException();
        }

        private IList ToList(IEnumerable<T> collection)
        {
            if (collection is IList)
                return (IList) collection;

            var list = new List<T>();
            list.AddRange(collection);
            return list;
        }

        private static bool IsCompatibleObject(object value)
        {
            return value is T || value == null && default(T) == null;
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            if (collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList changedItems, int index)
        {
            if (collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, changedItems, index));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index,
            int oldIndex)
        {
            if (collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index,
                    oldIndex));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem,
            int index)
        {
            if (collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem,
                    index));
        }

        private void OnCollectionReset()
        {
            if (collectionChanged != null)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        [Serializable]
        private class SimpleMonitor : IDisposable
        {
            private int _busyCount;
            public bool Busy => _busyCount > 0;

            public void Dispose()
            {
                --_busyCount;
            }

            public void Enter()
            {
                ++_busyCount;
            }
        }
    }
}