using System.Collections.Generic;
using UnityEngine;

namespace Zitga.CsvTools
{
    public abstract class SerializableDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver {
        [SerializeField]
        private List<K> keys = new List<K>();
        [SerializeField]
        private List<V> values = new List<V>();
 
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            this.Clear();
            for (int i = 0; i < this.keys.Count && i < this.values.Count; i++)
            {
                this[this.keys[i]] = this.values[i];
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.keys.Clear();
            this.values.Clear();

            foreach (var item in this)
            {
                this.keys.Add(item.Key);
                this.values.Add(item.Value);
            }
        }
    }
}
