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
using Loxodon.Framework.Services;

namespace Loxodon.Framework.Contexts
{
    public class Context : IDisposable
    {
        private static ApplicationContext context = new ApplicationContext();
        private static readonly Dictionary<string, Context> contexts = new Dictionary<string, Context>();
        private readonly Dictionary<string, object> attributes;
        private readonly IServiceContainer container;
        private readonly Context contextBase;

        private readonly bool innerContainer;

        public Context() : this(null, null)
        {
        }

        public Context(IServiceContainer container, Context contextBase)
        {
            attributes = new Dictionary<string, object>();
            this.contextBase = contextBase;
            this.container = container;
            if (this.container == null)
            {
                innerContainer = true;
                this.container = new ServiceContainer();
            }
        }

        public static ApplicationContext GetApplicationContext()
        {
            return context;
        }

        public static void SetApplicationContext(ApplicationContext context)
        {
            Context.context = context;
        }

        public static Context GetContext(string key)
        {
            Context context = null;
            contexts.TryGetValue(key, out context);
            return context;
        }

        public static T GetContext<T>(string key) where T : Context
        {
            return (T) GetContext(key);
        }

        public static void AddContext(string key, Context context)
        {
            contexts.Add(key, context);
        }

        public static void RemoveContext(string key)
        {
            contexts.Remove(key);
        }

        public virtual bool Contains(string name, bool cascade = true)
        {
            if (attributes.ContainsKey(name))
                return true;

            if (cascade && contextBase != null)
                return contextBase.Contains(name, cascade);

            return false;
        }

        public virtual object Get(string name, bool cascade = true)
        {
            return Get<object>(name, cascade);
        }

        public virtual T Get<T>(string name, bool cascade = true)
        {
            object v;
            if (attributes.TryGetValue(name, out v))
                return (T) v;

            if (cascade && contextBase != null)
                return contextBase.Get<T>(name, cascade);

            return default;
        }

        public virtual void Set(string name, object value)
        {
            Set<object>(name, value);
        }

        public virtual void Set<T>(string name, T value)
        {
            attributes[name] = value;
        }

        public virtual object Remove(string name)
        {
            return Remove<object>(name);
        }

        public virtual T Remove<T>(string name)
        {
            if (!attributes.ContainsKey(name))
                return default;

            object v = attributes[name];
            attributes.Remove(name);
            return (T) v;
        }

        public virtual IEnumerator GetEnumerator()
        {
            return attributes.GetEnumerator();
        }

        public virtual IServiceContainer GetContainer()
        {
            return container;
        }

        public virtual object GetService(Type type)
        {
            object result = container.Resolve(type);
            if (result != null)
                return result;

            if (contextBase != null)
                return contextBase.GetService(type);

            return null;
        }

        public virtual object GetService(string name)
        {
            object result = container.Resolve(name);
            if (result != null)
                return result;

            if (contextBase != null)
                return contextBase.GetService(name);

            return null;
        }

        public virtual T GetService<T>()
        {
            T result = container.Resolve<T>();
            if (result != null)
                return result;

            if (contextBase != null)
                return contextBase.GetService<T>();

            return default;
        }

        public virtual T GetService<T>(string name)
        {
            T result = container.Resolve<T>(name);
            if (result != null)
                return result;

            if (contextBase != null)
                return contextBase.GetService<T>(name);

            return default;
        }

        #region IDisposable Support

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    if (innerContainer && container != null)
                    {
                        IDisposable dis = container as IDisposable;
                        if (dis != null)
                            dis.Dispose();
                    }

                disposed = true;
            }
        }

        ~Context()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}