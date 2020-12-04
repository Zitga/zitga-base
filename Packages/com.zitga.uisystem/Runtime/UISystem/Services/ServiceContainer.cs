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

namespace Loxodon.Framework.Services
{
    public class ServiceContainer : IServiceContainer, IDisposable
    {
        private readonly Dictionary<string, IFactory> services = new Dictionary<string, IFactory>();

        public virtual object Resolve(Type type)
        {
            return Resolve<object>(type.Name);
        }

        public virtual T Resolve<T>()
        {
            return Resolve<T>(typeof(T).Name);
        }

        public virtual object Resolve(string name)
        {
            return Resolve<object>(name);
        }

        public virtual T Resolve<T>(string name)
        {
            if (services.TryGetValue(name, out IFactory factory))
                return (T) factory.Create();
            return default;
        }

        public virtual void Register<T>(Func<T> factory)
        {
            Register(typeof(T).Name, factory);
        }

        public virtual void Register(Type type, object target)
        {
            Register<object>(type.Name, target);
        }

        public virtual void Register(string name, object target)
        {
            Register<object>(name, target);
        }

        public virtual void Register<T>(T target)
        {
            Register(typeof(T).Name, target);
        }

        public virtual void Register<T>(string name, Func<T> factory)
        {
            if (services.ContainsKey(name))
                throw new DuplicateRegisterServiceException(string.Format("Duplicate key {0}", name));

            services.Add(name, new GenericFactory<T>(factory));
        }

        public virtual void Register<T>(string name, T target)
        {
            if (services.ContainsKey(name))
                throw new DuplicateRegisterServiceException(string.Format("Duplicate key {0}", name));

            services.Add(name, new SingleInstanceFactory(target));
        }

        public virtual void Unregister(Type type)
        {
            Unregister(type.Name);
        }

        public virtual void Unregister<T>()
        {
            Unregister(typeof(T).Name);
        }

        public virtual void Unregister(string name)
        {
            IFactory factory;
            if (services.TryGetValue(name, out factory))
                factory.Dispose();

            services.Remove(name);
        }

        internal interface IFactory : IDisposable
        {
            object Create();
        }

        internal class GenericFactory<T> : IFactory
        {
            private readonly Func<T> func;

            public GenericFactory(Func<T> func)
            {
                this.func = func;
            }

            public virtual object Create()
            {
                return func();
            }

            public void Dispose()
            {
            }
        }

        internal class SingleInstanceFactory : IFactory
        {
            private object target;

            public SingleInstanceFactory(object target)
            {
                this.target = target;
            }

            public virtual object Create()
            {
                return target;
            }

            #region IDisposable Support

            private bool disposed;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        IDisposable disposable = target as IDisposable;
                        if (disposable != null)
                            disposable.Dispose();
                        target = null;
                    }

                    disposed = true;
                }
            }

            ~SingleInstanceFactory()
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

        #region IDisposable Support

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (var kv in services)
                        kv.Value.Dispose();

                    services.Clear();
                    //this.services = null;
                }

                disposed = true;
            }
        }

        ~ServiceContainer()
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