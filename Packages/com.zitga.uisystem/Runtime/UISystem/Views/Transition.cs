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
using Loxodon.Framework.Execution;
using Loxodon.Log;

namespace Loxodon.Framework.Views
{
    public abstract class Transition : ITransition
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Transition));
        private bool animationDisabled;

        //bind the StateChange event.
        private bool bound;
        private bool done;
        private int layer;
        private Action onFinish;
        private Action onStart;
        private Action<IWindow, WindowState> onStateChanged;

        private bool running;

        private IManageable window;

        public Transition(IManageable window)
        {
            this.window = window;
        }

        public virtual IManageable Window
        {
            get => window;
            set => window = value;
        }

        public virtual bool AnimationDisabled
        {
            get => animationDisabled;
            protected set => animationDisabled = value;
        }

        public virtual int Layer
        {
            get => layer;
            protected set => layer = value;
        }

        public virtual Func<IWindow, IWindow, ActionType> OverlayPolicy { get; protected set; }

        public virtual bool IsDone
        {
            get => done;
            protected set => done = value;
        }

        public virtual object WaitForDone()
        {
            return Executors.WaitWhile(() => !IsDone);
        }

        public ITransition DisableAnimation(bool disabled)
        {
            if (running)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The transition is running.DisableAnimation failed.");

                return this;
            }

            animationDisabled = disabled;
            return this;
        }

        public ITransition AtLayer(int newLayer)
        {
            if (running)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The transition is running.sets the layer failed.");

                return this;
            }

            this.layer = newLayer;
            return this;
        }

        public ITransition Overlay(Func<IWindow, IWindow, ActionType> policy)
        {
            if (running)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The transition is running.sets the policy failed.");

                return this;
            }

            OverlayPolicy = policy;
            return this;
        }

        public ITransition OnStart(Action callback)
        {
            if (running)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The transition is running.OnStart failed.");

                return this;
            }

            onStart += callback;
            return this;
        }

        public ITransition OnStateChanged(Action<IWindow, WindowState> callback)
        {
            if (running)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The transition is running.OnStateChanged failed.");

                return this;
            }

            onStateChanged += callback;
            return this;
        }

        public ITransition OnFinish(Action callback)
        {
            if (done)
            {
                callback();
                return this;
            }

            onFinish += callback;
            return this;
        }

        ~Transition()
        {
            Unbind();
        }

        protected virtual void Bind()
        {
            if (bound)
                return;

            bound = true;
            if (window != null)
                window.StateChanged += StateChanged;
        }

        protected virtual void Unbind()
        {
            if (!bound)
                return;

            bound = false;

            if (window != null)
                window.StateChanged -= StateChanged;
        }

        protected void StateChanged(object sender, WindowStateEventArgs e)
        {
            RaiseStateChanged((IWindow) sender, e.State);
        }

        protected virtual void RaiseStart()
        {
            try
            {
                if (onStart != null)
                    onStart();
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.Warn("", e);
            }
        }

        protected virtual void RaiseStateChanged(IWindow window, WindowState state)
        {
            try
            {
                if (onStateChanged != null)
                    onStateChanged(window, state);
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.Warn("", e);
            }
        }

        protected virtual void RaiseFinished()
        {
            try
            {
                if (onFinish != null)
                    onFinish();
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.Warn("", e);
            }
        }

        protected virtual void OnStart()
        {
            Bind();
            RaiseStart();
        }

        protected virtual void OnEnd()
        {
            done = true;
            RaiseFinished();
            Unbind();
        }

        public virtual IEnumerator TransitionTask()
        {
            running = true;
            OnStart();
            yield return DoTransition();
            OnEnd();
        }

        protected abstract IEnumerator DoTransition();
    }

}