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
using UnityEngine;

namespace Loxodon.Framework.Views.Animations
{
    public enum AnimationType
    {
        EnterAnimation,
        ExitAnimation,
        ActivationAnimation,
        PassivationAnimation
    }

    public abstract class UIAnimation : MonoBehaviour, IAnimation
    {
        [SerializeField] private AnimationType animationType;

        private Action _onEnd;
        private Action _onStart;

        public AnimationType AnimationType
        {
            get => animationType;
            set => animationType = value;
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

        public abstract IAnimation Play();

        protected void OnStart()
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

        protected void OnEnd()
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