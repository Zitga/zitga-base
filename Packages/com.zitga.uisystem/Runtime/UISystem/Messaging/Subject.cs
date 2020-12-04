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
using System.Collections.Concurrent;
using System.Threading;

namespace Loxodon.Framework.Messaging
{
    public abstract class SubjectBase
    {
        public abstract void Publish(object message);
    }

    public class Subject<T> : SubjectBase
    {
        private readonly ConcurrentDictionary<string, WeakReference<Subscription>> subscriptions =
            new ConcurrentDictionary<string, WeakReference<Subscription>>();

        public bool IsEmpty()
        {
            return subscriptions.Count <= 0;
        }

        public override void Publish(object message)
        {
            Publish((T) message);
        }

        public void Publish(T message)
        {
            if (subscriptions.Count <= 0)
                return;

            foreach (var kv in subscriptions)
            {
                Subscription subscription;
                kv.Value.TryGetTarget(out subscription);
                if (subscription != null)
                    subscription.Publish(message);
            }
        }

        public ISubscription<T> Subscribe(Action<T> action)
        {
            return new Subscription(this, action);
        }

        private void Add(Subscription subscription)
        {
            var reference = new WeakReference<Subscription>(subscription, false);
            subscriptions.TryAdd(subscription.Key, reference);
        }

        private void Remove(Subscription subscription)
        {
            subscriptions.TryRemove(subscription.Key, out _);
        }

        private class Subscription : ISubscription<T>
        {
            private Action<T> action;
            private SynchronizationContext context;
            private Subject<T> subject;

            public Subscription(Subject<T> subject, Action<T> action)
            {
                this.subject = subject;
                this.action = action;
                Key = Guid.NewGuid().ToString();
                this.subject.Add(this);
            }

            public string Key { get; }

            public ISubscription<T> ObserveOn(SynchronizationContext context)
            {
                this.context = context ?? throw new ArgumentNullException("context");
                return this;
            }

            public void Publish(T message)
            {
                try
                {
                    if (context != null)
                        context.Post(state => action(message), null);
                    else
                        action(message);
                }
                catch (Exception)
                {
                }
            }

            #region IDisposable Support

            private bool disposed;

            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                try
                {
                    if (disposed)
                        return;

                    if (subject != null)
                        subject.Remove(this);

                    context = null;
                    action = null;
                    subject = null;
                }
                catch (Exception)
                {
                }

                disposed = true;
            }

            ~Subscription()
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
}