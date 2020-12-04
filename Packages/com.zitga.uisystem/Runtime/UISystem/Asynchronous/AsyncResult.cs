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
using System.Threading;
using Loxodon.Framework.Execution;

namespace Loxodon.Framework.Asynchronous
{
    public class AsyncResult : IAsyncResult, IPromise
    {
        protected readonly object _lock = new object();
        private Callbackable callbackable;
        protected bool cancelable;
        protected bool cancellationRequested;

        private bool cancelled;
        //private static readonly ILog log = LogManager.GetLogger(typeof(AsyncResult));

        private bool done;
        private Exception exception;
        private object result;

        private Synchronizable synchronizable;

        public AsyncResult() : this(false)
        {
        }

        public AsyncResult(bool cancelable)
        {
            this.cancelable = cancelable;
        }

        /// <summary>
        ///     Exception
        /// </summary>
        public virtual Exception Exception => exception;

        /// <summary>
        ///     Returns  "true" if this task finished.
        /// </summary>
        public virtual bool IsDone => done;

        /// <summary>
        ///     The execution result
        /// </summary>
        public virtual object Result => result;

        /// <summary>
        ///     Returns "true" if this task was cancelled before it completed normally.
        /// </summary>
        public virtual bool IsCancelled => cancelled;

        /// <summary>
        ///     Attempts to cancel execution of this task.  This attempt will
        ///     fail if the task has already completed, has already been cancelled,
        ///     or could not be cancelled for some other reason.If successful,
        ///     and this task has not started when "Cancel" is called,
        ///     this task should never run.
        /// </summary>
        /// <exception cref="NotSupportedException">If not supported, throw an exception.</exception>
        /// <returns></returns>
        public virtual bool Cancel()
        {
            if (!cancelable)
                throw new NotSupportedException();

            if (IsDone)
                return false;

            cancellationRequested = true;
            SetCancelled();
            return true;
        }

        public virtual ICallbackable Callbackable()
        {
            lock (_lock)
            {
                return callbackable ?? (callbackable = new Callbackable(this));
            }
        }

        public virtual ISynchronizable Synchronized()
        {
            lock (_lock)
            {
                return synchronizable ?? (synchronizable = new Synchronizable(this, _lock));
            }
        }

        /// <summary>
        ///     Wait for the result,suspends the coroutine.
        ///     eg:
        ///     IAsyncResult result;
        ///     yiled return result.WaitForDone();
        /// </summary>
        /// <returns></returns>
        public virtual object WaitForDone()
        {
            return Executors.WaitWhile(() => !IsDone);
        }

        public virtual bool IsCancellationRequested => cancellationRequested;

        public virtual void SetException(string error)
        {
            if (done)
                return;

            Exception exception = new Exception(string.IsNullOrEmpty(error) ? "unknown error!" : error);
            SetException(exception);
        }

        public virtual void SetException(Exception exception)
        {
            lock (_lock)
            {
                if (done)
                    return;

                this.exception = exception;
                done = true;
                Monitor.PulseAll(_lock);
            }

            RaiseOnCallback();
        }

        public virtual void SetResult(object result = null)
        {
            lock (_lock)
            {
                if (done)
                    return;

                this.result = result;
                done = true;
                Monitor.PulseAll(_lock);
            }

            RaiseOnCallback();
        }

        public virtual void SetCancelled()
        {
            lock (_lock)
            {
                if (!cancelable || done)
                    return;

                cancelled = true;
                exception = new OperationCanceledException();
                done = true;
                Monitor.PulseAll(_lock);
            }

            RaiseOnCallback();
        }

        protected virtual void RaiseOnCallback()
        {
            if (callbackable != null)
                callbackable.RaiseOnCallback();
        }
    }

    public class AsyncResult<TResult> : AsyncResult, IAsyncResult<TResult>, IPromise<TResult>
    {
        private Callbackable<TResult> callbackable;
        //private static readonly ILog log = LogManager.GetLogger(typeof(AsyncResult<TResult>));

        private Synchronizable<TResult> synchronizable;

        public AsyncResult() : this(false)
        {
        }

        public AsyncResult(bool cancelable) : base(cancelable)
        {
        }

        /// <summary>
        ///     The execution result
        /// </summary>
        public new virtual TResult Result
        {
            get
            {
                object result = base.Result;
                return result != null ? (TResult) result : default;
            }
        }

        public new virtual ICallbackable<TResult> Callbackable()
        {
            lock (_lock)
            {
                return callbackable ?? (callbackable = new Callbackable<TResult>(this));
            }
        }

        public new virtual ISynchronizable<TResult> Synchronized()
        {
            lock (_lock)
            {
                return synchronizable ?? (synchronizable = new Synchronizable<TResult>(this, _lock));
            }
        }

        public virtual void SetResult(TResult result)
        {
            base.SetResult(result);
        }

        protected override void RaiseOnCallback()
        {
            base.RaiseOnCallback();
            if (callbackable != null)
                callbackable.RaiseOnCallback();
        }
    }
}