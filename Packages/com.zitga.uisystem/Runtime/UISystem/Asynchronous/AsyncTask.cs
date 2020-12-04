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
using System.Threading;
using Loxodon.Framework.Execution;
using Loxodon.Log;
#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace Loxodon.Framework.Asynchronous
{
    public class AsyncTask : IAsyncTask
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AsyncTask));

        private readonly Action action;

        private Action<Exception> errorCallbackOnMainThread;
        private Action<Exception> errorCallbackOnWorkerThread;

        private Action finishCallbackOnMainThread;
        private Action finishCallbackOnWorkerThread;

        private Action postCallbackOnMainThread;
        private Action postCallbackOnWorkerThread;

        private Action preCallbackOnMainThread;
        private Action preCallbackOnWorkerThread;
        private readonly AsyncResult result;

        private int running;

        /// <summary>
        /// </summary>
        /// <param name="task"></param>
        /// <param name="runOnMainThread"></param>
        public AsyncTask(Action task, bool runOnMainThread = false)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            result = new AsyncResult();
            if (runOnMainThread)
                action = WrapAction(() =>
                {
                    Executors.RunOnMainThread(task, true);
                    result.SetResult();
                });
            else
                action = WrapAction(() =>
                {
                    task();
                    result.SetResult();
                });
        }

        /// <summary>
        /// </summary>
        /// <param name="task"></param>
        /// <param name="runOnMainThread"></param>
        public AsyncTask(Action<IPromise> task, bool runOnMainThread = false, bool cancelable = false)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            result = new AsyncResult(!runOnMainThread && cancelable);
            if (runOnMainThread)
                action = WrapAction(() =>
                {
                    Executors.RunOnMainThread(() => task(result), true);
                    result.Synchronized().WaitForResult();
                });
            else
                action = WrapAction(() =>
                {
                    task(result);
                    result.Synchronized().WaitForResult();
                });
        }

        /// <summary>
        ///     run on main thread
        /// </summary>
        /// <param name="task"></param>
        public AsyncTask(IEnumerator task, bool cancelable = false)
        {
            if (task == null)
                throw new ArgumentNullException("task");

            result = new AsyncResult(cancelable);
            action = WrapAction(() =>
            {
                Executors.RunOnCoroutine(task, result);
                result.Synchronized().WaitForResult();
            });
        }

        public virtual object Result => result.Result;

        public virtual Exception Exception => result.Exception;

        public virtual bool IsDone => result.IsDone && running == 0;

        public virtual bool IsCancelled => result.IsCancelled;

        public virtual bool Cancel()
        {
            return result.Cancel();
        }

        public virtual ICallbackable Callbackable()
        {
            return result.Callbackable();
        }

        public virtual ISynchronizable Synchronized()
        {
            return result.Synchronized();
        }

        public virtual object WaitForDone()
        {
            return Executors.WaitWhile(() => !IsDone);
        }

        public IAsyncTask OnPreExecute(Action callback, bool runOnMainThread = true)
        {
            if (runOnMainThread)
                preCallbackOnMainThread += callback;
            else
                preCallbackOnWorkerThread += callback;
            return this;
        }

        public IAsyncTask OnPostExecute(Action callback, bool runOnMainThread = true)
        {
            if (runOnMainThread)
                postCallbackOnMainThread += callback;
            else
                postCallbackOnWorkerThread += callback;
            return this;
        }

        public IAsyncTask OnError(Action<Exception> callback, bool runOnMainThread = true)
        {
            if (runOnMainThread)
                errorCallbackOnMainThread += callback;
            else
                errorCallbackOnWorkerThread += callback;
            return this;
        }

        public IAsyncTask OnFinish(Action callback, bool runOnMainThread = true)
        {
            if (runOnMainThread)
                finishCallbackOnMainThread += callback;
            else
                finishCallbackOnWorkerThread += callback;
            return this;
        }

        public IAsyncTask Start(int delay)
        {
            if (delay <= 0)
                return Start();

            Executors.RunAsyncNoReturn(() =>
            {
#if NETFX_CORE
                Task.Delay(delay).Wait();
#else
                Thread.Sleep(delay);
#endif
                if (IsDone || running == 1)
                    return;

                Start();
            });
            return this;
        }

        public IAsyncTask Start()
        {
            if (IsDone)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The task has been done!");

                return this;
            }

            if (Interlocked.CompareExchange(ref running, 1, 0) == 1)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The task is running!");

                return this;
            }

            try
            {
                if (preCallbackOnMainThread != null)
                    Executors.RunOnMainThread(preCallbackOnMainThread, true);
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("{0}", e);
            }

            Executors.RunAsync(action);

            return this;
        }

        protected virtual Action WrapAction(Action action)
        {
            Action wrapAction = () =>
            {
                try
                {
                    try
                    {
                        if (preCallbackOnWorkerThread != null)
                            preCallbackOnWorkerThread();
                    }
                    catch (Exception e)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("{0}", e);
                    }

                    if (result.IsCancellationRequested)
                    {
                        result.SetCancelled();
                        return;
                    }

                    action();
                }
                catch (Exception e)
                {
                    result.SetException(e);
                }
                finally
                {
                    try
                    {
                        if (Exception != null)
                        {
                            if (errorCallbackOnMainThread != null)
                                Executors.RunOnMainThread(() => errorCallbackOnMainThread(Exception), true);

                            if (errorCallbackOnWorkerThread != null)
                                errorCallbackOnWorkerThread(Exception);
                        }
                        else
                        {
                            if (postCallbackOnMainThread != null)
                                Executors.RunOnMainThread(postCallbackOnMainThread, true);

                            if (postCallbackOnWorkerThread != null)
                                postCallbackOnWorkerThread();
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("{0}", e);
                    }

                    try
                    {
                        if (finishCallbackOnMainThread != null)
                            Executors.RunOnMainThread(finishCallbackOnMainThread, true);

                        if (finishCallbackOnWorkerThread != null)
                            finishCallbackOnWorkerThread();
                    }
                    catch (Exception e)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("{0}", e);
                    }

                    Interlocked.Exchange(ref running, 0);
                }
            };
            return wrapAction;
        }
    }

    public class AsyncTask<TResult> : IAsyncTask<TResult>
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AsyncTask<TResult>));

        private readonly Action action;

        private Action<Exception> errorCallbackOnMainThread;
        private Action<Exception> errorCallbackOnWorkerThread;

        private Action finishCallbackOnMainThread;
        private Action finishCallbackOnWorkerThread;

        private Action<TResult> postCallbackOnMainThread;
        private Action<TResult> postCallbackOnWorkerThread;

        private Action preCallbackOnMainThread;
        private Action preCallbackOnWorkerThread;
        private readonly AsyncResult<TResult> result;

        private int running;

        /// <summary>
        /// </summary>
        /// <param name="task"></param>
        /// <param name="runOnMainThread"></param>
        public AsyncTask(Func<TResult> task, bool runOnMainThread = false)
        {
            if (task == null)
                throw new ArgumentNullException();

            result = new AsyncResult<TResult>();

            if (runOnMainThread)
                action = WrapAction(() => { return Executors.RunOnMainThread(task); });
            else
                action = WrapAction(() => task());
        }

        /// <summary>
        /// </summary>
        /// <param name="task"></param>
        /// <param name="runOnMainThread"></param>
        public AsyncTask(Action<IPromise<TResult>> task, bool runOnMainThread = false, bool cancelable = false)
        {
            if (task == null)
                throw new ArgumentNullException();

            result = new AsyncResult<TResult>(!runOnMainThread && cancelable);

            if (runOnMainThread)
                action = WrapAction(() =>
                {
                    Executors.RunOnMainThread(() => task(result));
                    return result.Synchronized().WaitForResult();
                });
            else
                action = WrapAction(() =>
                {
                    task(result);
                    return result.Synchronized().WaitForResult();
                });
        }

        /// <summary>
        /// </summary>
        /// <param name="task"></param>
        public AsyncTask(Func<IPromise<TResult>, IEnumerator> task, bool cancelable = false)
        {
            if (task == null)
                throw new ArgumentNullException();

            result = new AsyncResult<TResult>(cancelable);
            action = WrapAction(() =>
            {
                Executors.RunOnCoroutine(task(result), result);
                return result.Synchronized().WaitForResult();
            });
        }

        public virtual TResult Result => result.Result;

        object IAsyncResult.Result => result.Result;

        public virtual Exception Exception => result.Exception;

        public virtual bool IsDone => result.IsDone && running == 0;

        public virtual bool IsCancelled => result.IsCancelled;

        public virtual bool Cancel()
        {
            return result.Cancel();
        }

        public virtual ICallbackable<TResult> Callbackable()
        {
            return result.Callbackable();
        }

        public virtual ISynchronizable<TResult> Synchronized()
        {
            return result.Synchronized();
        }

        ICallbackable IAsyncResult.Callbackable()
        {
            return (result as IAsyncResult).Callbackable();
        }

        ISynchronizable IAsyncResult.Synchronized()
        {
            return (result as IAsyncResult).Synchronized();
        }

        public virtual object WaitForDone()
        {
            return Executors.WaitWhile(() => !IsDone);
        }

        public IAsyncTask<TResult> OnPreExecute(Action callback, bool runOnMainThread = true)
        {
            if (runOnMainThread)
                preCallbackOnMainThread += callback;
            else
                preCallbackOnWorkerThread += callback;
            return this;
        }

        public IAsyncTask<TResult> OnPostExecute(Action<TResult> callback, bool runOnMainThread = true)
        {
            if (runOnMainThread)
                postCallbackOnMainThread += callback;
            else
                postCallbackOnWorkerThread += callback;
            return this;
        }

        public IAsyncTask<TResult> OnError(Action<Exception> callback, bool runOnMainThread = true)
        {
            if (runOnMainThread)
                errorCallbackOnMainThread += callback;
            else
                errorCallbackOnWorkerThread += callback;
            return this;
        }

        public IAsyncTask<TResult> OnFinish(Action callback, bool runOnMainThread = true)
        {
            if (runOnMainThread)
                finishCallbackOnMainThread += callback;
            else
                finishCallbackOnWorkerThread += callback;
            return this;
        }

        public IAsyncTask<TResult> Start(int delay)
        {
            if (delay <= 0)
                return Start();

            Executors.RunAsyncNoReturn(() =>
            {
#if NETFX_CORE
                Task.Delay(delay).Wait();
#else
                Thread.Sleep(delay);
#endif
                if (IsDone || running == 1)
                    return;

                Start();
            });

            return this;
        }

        public IAsyncTask<TResult> Start()
        {
            if (IsDone)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The task has been done!");
                return this;
            }

            if (Interlocked.CompareExchange(ref running, 1, 0) == 1)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The task is running!");
                return this;
            }

            try
            {
                if (preCallbackOnMainThread != null)
                    Executors.RunOnMainThread(preCallbackOnMainThread, true);
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("{0}", e);
            }

            Executors.RunAsync(action);

            return this;
        }

        protected virtual Action WrapAction(Func<TResult> action)
        {
            Action wrapAction = () =>
            {
                try
                {
                    try
                    {
                        if (preCallbackOnWorkerThread != null)
                            preCallbackOnWorkerThread();
                    }
                    catch (Exception e)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("{0}", e);
                    }

                    if (result.IsCancellationRequested)
                    {
                        result.SetCancelled();
                        return;
                    }

                    TResult obj = action();
                    result.SetResult(obj);
                }
                catch (Exception e)
                {
                    result.SetException(e);
                }
                finally
                {
                    try
                    {
                        if (Exception != null)
                        {
                            if (errorCallbackOnMainThread != null)
                                Executors.RunOnMainThread(() => errorCallbackOnMainThread(Exception), true);

                            if (errorCallbackOnWorkerThread != null)
                                errorCallbackOnWorkerThread(Exception);
                        }
                        else
                        {
                            if (postCallbackOnMainThread != null)
                                Executors.RunOnMainThread(() => postCallbackOnMainThread(Result), true);

                            if (postCallbackOnWorkerThread != null)
                                postCallbackOnWorkerThread(Result);
                        }
                    }
                    catch (Exception e)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("{0}", e);
                    }

                    try
                    {
                        if (finishCallbackOnMainThread != null)
                            Executors.RunOnMainThread(finishCallbackOnMainThread, true);

                        if (finishCallbackOnWorkerThread != null)
                            finishCallbackOnWorkerThread();
                    }
                    catch (Exception e)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("{0}", e);
                    }

                    Interlocked.Exchange(ref running, 0);
                }
            };
            return wrapAction;
        }
    }
}