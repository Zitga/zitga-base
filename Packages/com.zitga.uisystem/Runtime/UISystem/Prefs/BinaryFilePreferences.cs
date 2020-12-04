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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Loxodon.Log;
using UnityEngine;

namespace Loxodon.Framework.Prefs
{
    /// <summary>
    /// </summary>
    public class BinaryFilePreferencesFactory : AbstractFactory
    {
        /// <summary>
        /// </summary>
        public BinaryFilePreferencesFactory() : this(null, null)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="serializer"></param>
        public BinaryFilePreferencesFactory(ISerializer serializer) : this(serializer, null)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="encryptor"></param>
        public BinaryFilePreferencesFactory(ISerializer serializer, IEncryptor encryptor) : base(serializer, encryptor)
        {
        }

        /// <summary>
        ///     Create an instance of the BinaryFilePreferences.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Preferences Create(string name)
        {
            return new BinaryFilePreferences(name, Serializer, Encryptor);
        }
    }

    /// <summary>
    /// </summary>
    public class BinaryFilePreferences : Preferences
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BinaryFilePreferences));

        /// <summary>
        ///     cache.
        /// </summary>
        protected readonly Dictionary<string, string> dict = new Dictionary<string, string>();

        /// <summary>
        ///     encryptor
        /// </summary>
        protected readonly IEncryptor encryptor;

        /// <summary>
        ///     serializer
        /// </summary>
        protected readonly ISerializer serializer;

        private readonly string root;

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="serializer"></param>
        /// <param name="encryptor"></param>
        public BinaryFilePreferences(string name, ISerializer serializer, IEncryptor encryptor) : base(name)
        {
            root = Application.persistentDataPath;
            this.serializer = serializer;
            this.encryptor = encryptor;
            Load();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public virtual StringBuilder GetDirectory()
        {
            StringBuilder buf = new StringBuilder(root);
            buf.Append("/").Append(Name).Append("/");
            return buf;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public virtual StringBuilder GetFullFileName()
        {
            return GetDirectory().Append("prefs.dat");
        }

        /// <summary>
        /// </summary>
        protected override void Load()
        {
            try
            {
                var filename = GetFullFileName().ToString();
                if (!File.Exists(filename))
                    return;

                var data = File.ReadAllBytes(filename);
                if (data == null || data.Length <= 0)
                    return;

                if (encryptor != null)
                    data = encryptor.Decode(data);

                dict.Clear();
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        var count = reader.ReadInt32();
                        for (var i = 0; i < count; i++)
                        {
                            var key = reader.ReadString();
                            var value = reader.ReadString();
                            dict.Add(key, value);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("Load failed,{0}", e);
            }
        }

        public override object GetObject(string key, Type type, object defaultValue)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;

            var str = dict[key];
            if (string.IsNullOrEmpty(str))
                return defaultValue;

            return serializer.Deserialize(str, type);
        }

        public override void SetObject(string key, object value)
        {
            if (value == null)
            {
                dict.Remove(key);
                return;
            }

            dict[key] = serializer.Serialize(value);
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override T GetObject<T>(string key, T defaultValue)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;

            var str = dict[key];
            if (string.IsNullOrEmpty(str))
                return defaultValue;

            return (T) serializer.Deserialize(str, typeof(T));
        }

        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetObject<T>(string key, T value)
        {
            if (value == null)
            {
                dict.Remove(key);
                return;
            }

            dict[key] = serializer.Serialize(value);
        }

        public override object[] GetArray(string key, Type type, object[] defaultValue)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;

            var str = dict[key];
            if (string.IsNullOrEmpty(str))
                return defaultValue;

            var items = str.Split(ARRAY_SEPARATOR);
            var list = new List<object>();
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (string.IsNullOrEmpty(item))
                    list.Add(null);
                else
                    list.Add(serializer.Deserialize(items[i], type));
            }

            return list.ToArray();
        }

        public override void SetArray(string key, object[] values)
        {
            if (values == null || values.Length == 0)
            {
                dict.Remove(key);
                return;
            }

            StringBuilder buf = new StringBuilder();
            for (var i = 0; i < values.Length; i++)
            {
                object value = values[i];
                buf.Append(serializer.Serialize(value));
                if (i < values.Length - 1)
                    buf.Append(ARRAY_SEPARATOR);
            }

            dict[key] = buf.ToString();
        }

        public override T[] GetArray<T>(string key, T[] defaultValue)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;

            var str = dict[key];
            if (string.IsNullOrEmpty(str))
                return defaultValue;

            var items = str.Split(ARRAY_SEPARATOR);
            var list = new List<T>();
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (string.IsNullOrEmpty(item))
                    list.Add(default);
                else
                    list.Add((T) serializer.Deserialize(items[i], typeof(T)));
            }

            return list.ToArray();
        }

        public override void SetArray<T>(string key, T[] values)
        {
            if (values == null || values.Length == 0)
            {
                dict.Remove(key);
                return;
            }

            StringBuilder buf = new StringBuilder();
            for (var i = 0; i < values.Length; i++)
            {
                T value = values[i];
                buf.Append(serializer.Serialize(value));
                if (i < values.Length - 1)
                    buf.Append(ARRAY_SEPARATOR);
            }

            dict[key] = buf.ToString();
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool ContainsKey(string key)
        {
            return dict.ContainsKey(key);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        public override void Remove(string key)
        {
            if (dict.ContainsKey(key))
                dict.Remove(key);
        }

        /// <summary>
        /// </summary>
        public override void RemoveAll()
        {
            dict.Clear();
        }

        /// <summary>
        /// </summary>
        public override void Save()
        {
            if (dict.Count <= 0)
            {
                Delete();
                return;
            }

            Directory.CreateDirectory(GetDirectory().ToString());
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(dict.Count);
                    foreach (var kv in dict)
                    {
                        writer.Write(kv.Key);
                        writer.Write(kv.Value);
                    }

                    writer.Flush();
                }

                var data = stream.ToArray();
                if (encryptor != null)
                    data = encryptor.Encode(data);

                var filename = GetFullFileName().ToString();
                File.WriteAllBytes(filename, data);
            }
        }

        /// <summary>
        /// </summary>
        public override void Delete()
        {
            dict.Clear();
            var filename = GetFullFileName().ToString();
            if (File.Exists(filename))
                File.Delete(filename);
        }
    }
}