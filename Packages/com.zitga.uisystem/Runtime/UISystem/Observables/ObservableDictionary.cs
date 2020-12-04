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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using INotifyCollectionChanged = System.Collections.Specialized.INotifyCollectionChanged;
using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;
using NotifyCollectionChangedEventArgs = System.Collections.Specialized.NotifyCollectionChangedEventArgs;
using NotifyCollectionChangedEventHandler = System.Collections.Specialized.NotifyCollectionChangedEventHandler;

namespace Loxodon.Framework.Observables
{
    [Serializable]
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs CountEventArgs = new PropertyChangedEventArgs("Count");
        private static readonly PropertyChangedEventArgs IndexerEventArgs = new PropertyChangedEventArgs("Item[]");
        private static readonly PropertyChangedEventArgs KeysEventArgs = new PropertyChangedEventArgs("Keys");
        private static readonly PropertyChangedEventArgs ValuesEventArgs = new PropertyChangedEventArgs("Values");
        private readonly object collectionChangedLock = new object();

        private readonly object propertyChangedLock = new object();
        private NotifyCollectionChangedEventHandler collectionChanged;

        protected Dictionary<TKey, TValue> dictionary;
        private PropertyChangedEventHandler propertyChanged;

        public ObservableDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        public ObservableDictionary(int capacity)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        object IDictionary.this[object key]
        {
            get => ((IDictionary) dictionary)[key];
            set => Insert((TKey) key, (TValue) value, false);
        }

        ICollection IDictionary.Keys => ((IDictionary) dictionary).Keys;

        ICollection IDictionary.Values => ((IDictionary) dictionary).Values;

        bool IDictionary.Contains(object key)
        {
            return ((IDictionary) dictionary).Contains(key);
        }

        void IDictionary.Add(object key, object value)
        {
            Add((TKey) key, (TValue) value);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary) dictionary).GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            Remove((TKey) key);
        }

        bool IDictionary.IsFixedSize => ((IDictionary) dictionary).IsFixedSize;

        void ICollection.CopyTo(Array array, int index)
        {
            ((IDictionary) dictionary).CopyTo(array, index);
        }

        object ICollection.SyncRoot => ((IDictionary) dictionary).SyncRoot;

        bool ICollection.IsSynchronized => ((IDictionary) dictionary).IsSynchronized;

        public TValue this[TKey key]
        {
            get
            {
                if (!dictionary.ContainsKey(key))
                    return default;
                return dictionary[key];
            }
            set => Insert(key, value, false);
        }

        public ICollection<TKey> Keys => dictionary.Keys;

        public ICollection<TValue> Values => dictionary.Values;

        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            TValue value;
            dictionary.TryGetValue(key, out value);
            var removed = dictionary.Remove(key);
            if (removed)
            {
                OnPropertyChanged(NotifyCollectionChangedAction.Remove);
                if (collectionChanged != null)
                    OnCollectionChanged(NotifyCollectionChangedAction.Remove,
                        new KeyValuePair<TKey, TValue>(key, value));
            }

            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Insert(item.Key, item.Value, true);
        }

        public void Clear()
        {
            if (dictionary.Count > 0)
            {
                dictionary.Clear();
                OnPropertyChanged(NotifyCollectionChangedAction.Reset);
                if (collectionChanged != null)
                    OnCollectionChanged();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary) dictionary).CopyTo(array, arrayIndex);
        }

        public int Count => dictionary.Count;

        public bool IsReadOnly => ((IDictionary) dictionary).IsReadOnly;

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) dictionary).GetEnumerator();
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

        public void AddRange(IDictionary<TKey, TValue> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            if (items.Count > 0)
            {
                if (dictionary.Count > 0)
                {
                    if (items.Keys.Any(k => dictionary.ContainsKey(k)))
                        throw new ArgumentException("An item with the same key has already been added.");
                    foreach (var item in items)
                        ((IDictionary<TKey, TValue>) dictionary).Add(item);
                }
                else
                {
                    dictionary = new Dictionary<TKey, TValue>(items);
                }

                OnPropertyChanged(NotifyCollectionChangedAction.Add);
                if (collectionChanged != null)
                    OnCollectionChanged(NotifyCollectionChangedAction.Add, items.ToArray());
            }
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            TValue item;
            if (dictionary.TryGetValue(key, out item))
            {
                if (add)
                    throw new ArgumentException("An item with the same key has already been added.");

                if (Equals(item, value))
                    return;

                dictionary[key] = value;
                OnPropertyChanged(NotifyCollectionChangedAction.Replace);
                if (collectionChanged != null)
                    OnCollectionChanged(NotifyCollectionChangedAction.Replace,
                        new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, item));
            }
            else
            {
                dictionary[key] = value;
                OnPropertyChanged(NotifyCollectionChangedAction.Add);
                if (collectionChanged != null)
                    OnCollectionChanged(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        private void OnPropertyChanged(NotifyCollectionChangedAction action)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                {
                    OnPropertyChanged(CountEventArgs);
                    OnPropertyChanged(IndexerEventArgs);
                    OnPropertyChanged(KeysEventArgs);
                    OnPropertyChanged(ValuesEventArgs);
                    break;
                }
                case NotifyCollectionChangedAction.Replace:
                {
                    OnPropertyChanged(IndexerEventArgs);
                    OnPropertyChanged(ValuesEventArgs);
                    break;
                }
                case NotifyCollectionChangedAction.Move:
                default:
                {
                    OnPropertyChanged(CountEventArgs);
                    OnPropertyChanged(IndexerEventArgs);
                    OnPropertyChanged(KeysEventArgs);
                    OnPropertyChanged(ValuesEventArgs);
                    break;
                }
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs eventArgs)
        {
            propertyChanged?.Invoke(this, eventArgs);
        }

        private void OnCollectionChanged()
        {
            collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> changedItem)
        {
            collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, KeyValuePair<TKey, TValue> newItem,
            KeyValuePair<TKey, TValue> oldItem)
        {
            collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList newItems)
        {
            collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItems));
        }
    }
}