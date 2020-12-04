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
using Loxodon.Log;
using UnityEngine;

namespace Loxodon.Framework.Execution
{
    /// <summary>
    ///     Interceptable enumerator
    /// </summary>
    public class InterceptableEnumerator : IEnumerator
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(InterceptableEnumerator));

        private Func<bool> hasNext;

        private Action<Exception> onException;
        private Action onFinally;
        private readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();

        public InterceptableEnumerator(IEnumerator routine)
        {
            stack.Push(routine);
        }

        public object Current { get; private set; }

        public bool MoveNext()
        {
            try
            {
                if (!HasNext())
                {
                    OnFinally();
                    return false;
                }

                if (stack.Count <= 0)
                {
                    OnFinally();
                    return false;
                }

                IEnumerator ie = stack.Peek();
                var hasNext = ie.MoveNext();
                if (!hasNext)
                {
                    stack.Pop();
                    return MoveNext();
                }

                Current = ie.Current;
                if (Current is IEnumerator)
                {
                    stack.Push(Current as IEnumerator);
                    return MoveNext();
                }

                if (Current is Coroutine && log.IsWarnEnabled)
                    log.Warn(
                        "The Enumerator's results contains the 'UnityEngine.Coroutine' type,If occurs an exception,it can't be catched.It is recommended to use 'yield return routine',rather than 'yield return StartCoroutine(routine)'.");

                return true;
            }
            catch (Exception e)
            {
                OnException(e);
                OnFinally();
                return false;
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        private void OnException(Exception e)
        {
            try
            {
                if (onException == null)
                    return;

                foreach (Action<Exception> action in onException.GetInvocationList())
                    try
                    {
                        action(e);
                    }
                    catch (Exception ex)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("{0}", ex);
                    }
            }
            catch (Exception)
            {
            }
        }

        private void OnFinally()
        {
            try
            {
                if (onFinally == null)
                    return;

                foreach (Action action in onFinally.GetInvocationList())
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("{0}", ex);
                    }
            }
            catch (Exception)
            {
            }
        }

        private bool HasNext()
        {
            if (hasNext == null)
                return true;
            return hasNext();
        }

        /// <summary>
        ///     Register a condition code block.
        /// </summary>
        /// <param name="hasNext"></param>
        public virtual void RegisterConditionBlock(Func<bool> hasNext)
        {
            this.hasNext = hasNext;
        }

        /// <summary>
        ///     Register a code block, when an exception occurs it will be executed.
        /// </summary>
        /// <param name="onException"></param>
        public virtual void RegisterCatchBlock(Action<Exception> onException)
        {
            this.onException += onException;
        }

        /// <summary>
        ///     Register a code block, when the end of the operation is executed.
        /// </summary>
        /// <param name="onFinally"></param>
        public virtual void RegisterFinallyBlock(Action onFinally)
        {
            this.onFinally += onFinally;
        }
    }
}