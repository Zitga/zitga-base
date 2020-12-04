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

namespace Loxodon.Framework.Asynchronous
{
    public class ProgressResult<TProgress> : AsyncResult, IProgressResult<TProgress>, IProgressPromise<TProgress>
    {
        protected TProgress _progress;
        private ProgressCallbackable<TProgress> callbackable;

        public ProgressResult() : this(false)
        {
        }

        public ProgressResult(bool cancelable) : base(cancelable)
        {
        }

        public virtual void UpdateProgress(TProgress progress)
        {
            _progress = progress;
            RaiseOnProgressCallback(progress);
        }

        /// <summary>
        ///     The task's progress.
        /// </summary>
        public virtual TProgress Progress => _progress;

        public new virtual IProgressCallbackable<TProgress> Callbackable()
        {
            lock (_lock)
            {
                return callbackable ?? (callbackable = new ProgressCallbackable<TProgress>(this));
            }
        }

        protected override void RaiseOnCallback()
        {
            base.RaiseOnCallback();
            if (callbackable != null)
                callbackable.RaiseOnCallback();
        }

        protected virtual void RaiseOnProgressCallback(TProgress progress)
        {
            if (callbackable != null)
                callbackable.RaiseOnProgressCallback(progress);
        }
    }

    public class ProgressResult<TProgress, TResult> : ProgressResult<TProgress>, IProgressResult<TProgress, TResult>,
        IProgressPromise<TProgress, TResult>
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof(ProgressResult<TProgress, TResult>));

        private Callbackable<TResult> callbackable;
        private ProgressCallbackable<TProgress, TResult> progressCallbackable;
        private Synchronizable<TResult> synchronizable;

        public ProgressResult() : this(false)
        {
        }

        public ProgressResult(bool cancelable) : base(cancelable)
        {
        }

        public virtual void SetResult(TResult result)
        {
            base.SetResult(result);
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

        public new virtual IProgressCallbackable<TProgress, TResult> Callbackable()
        {
            lock (_lock)
            {
                return progressCallbackable ??
                       (progressCallbackable = new ProgressCallbackable<TProgress, TResult>(this));
            }
        }

        public new virtual ISynchronizable<TResult> Synchronized()
        {
            lock (_lock)
            {
                return synchronizable ?? (synchronizable = new Synchronizable<TResult>(this, _lock));
            }
        }

        ICallbackable<TResult> IAsyncResult<TResult>.Callbackable()
        {
            lock (_lock)
            {
                return callbackable ?? (callbackable = new Callbackable<TResult>(this));
            }
        }

        protected override void RaiseOnCallback()
        {
            base.RaiseOnCallback();
            if (callbackable != null)
                callbackable.RaiseOnCallback();
            if (progressCallbackable != null)
                progressCallbackable.RaiseOnCallback();
        }

        protected override void RaiseOnProgressCallback(TProgress progress)
        {
            base.RaiseOnProgressCallback(progress);
            if (progressCallbackable != null)
                progressCallbackable.RaiseOnProgressCallback(progress);
        }
    }
}