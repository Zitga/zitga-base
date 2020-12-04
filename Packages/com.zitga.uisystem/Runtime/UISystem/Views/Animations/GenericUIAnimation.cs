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

namespace Loxodon.Framework.Views.Animations
{
    public delegate void AnimationAction<T>(T view, Action startCallback, Action endCallback) where T : IUIView;

    public class GenericUIAnimation<T> : IAnimation where T : IUIView
    {
        private Action _onEnd;

        private Action _onStart;
        private readonly AnimationAction<T> animation;
        private readonly T view;

        public GenericUIAnimation(T view, AnimationAction<T> animation)
        {
            this.view = view;
            this.animation = animation;
        }

        public IAnimation OnStart(Action onStart)
        {
            _onStart += onStart;
            return this;
        }

        public IAnimation OnEnd(Action onEnd)
        {
            _onEnd += onEnd;
            return this;
        }

        public virtual IAnimation Play()
        {
            if (animation != null)
                animation(view, OnStart, OnEnd);
            return this;
        }

        protected virtual void OnStart()
        {
            try
            {
                if (_onStart != null)
                {
                    _onStart();
                    _onStart = null;
                }
            }
            catch (Exception)
            {
            }
        }

        protected virtual void OnEnd()
        {
            try
            {
                if (_onEnd != null)
                {
                    _onEnd();
                    _onEnd = null;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}