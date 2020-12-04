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
using System.Diagnostics.CodeAnalysis;
using Loxodon.Framework.Asynchronous;
using Loxodon.Log;
using UnityEngine;
using IAsyncResult = Loxodon.Framework.Asynchronous.IAsyncResult;
using IPromise = Loxodon.Framework.Asynchronous.IPromise;

namespace Loxodon.Framework.Views
{
    public abstract class Window : Window<AWindowProperties>{}
    
    [DisallowMultipleComponent]
    [SuppressMessage("ReSharper", "DelegateSubtraction")]
    public abstract class Window<TProps> : WindowView, IManageable where TProps : AWindowProperties
    {
        protected static readonly ILog log = LogManager.GetLogger(typeof(Window<TProps>));

        [SerializeField] private WindowType windowType = WindowType.FULL;

        [SerializeField] [Range(0, 10)] private int windowPriority;

        private readonly object _lock = new object();
        private bool activated;
        private TProps properties;
        private EventHandler activatedChanged;
        private ITransition dismissTransition;
        private EventHandler onDismissed;
        private WindowState state = WindowState.NONE;
        private EventHandler<WindowStateEventArgs> stateChanged;
        private EventHandler visibilityChanged;
        private IWindowManager windowManager;

        private GlobalWindowManager globalWindowManager;

        protected WindowState State
        {
            get => state;
            set
            {
                if (state.Equals(value))
                    return;

                state = value;
                RaiseStateChanged(state);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RaiseVisibilityChanged();
        }

        protected override void OnDisable()
        {
            RaiseVisibilityChanged();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (!Dismissed && dismissTransition == null) Dismiss(true);
            base.OnDestroy();
        }

        /// <summary>
        ///     Activate
        /// </summary>
        /// <returns></returns>
        public virtual IAsyncResult Activate(bool ignoreAnimation)
        {
            AsyncResult result = new AsyncResult();
            try
            {
                if (!Visibility)
                {
                    result.SetException(new InvalidOperationException("The window is not visible."));
                    return result;
                }

                if (Activated)
                {
                    result.SetResult();
                    return result;
                }

                if (!ignoreAnimation && ActivationAnimation != null)
                {
                    ActivationAnimation.OnStart(() => { State = WindowState.ACTIVATION_ANIMATION_BEGIN; }).OnEnd(() =>
                    {
                        State = WindowState.ACTIVATION_ANIMATION_END;
                        Activated = true;
                        State = WindowState.ACTIVATED;
                        result.SetResult();
                    }).Play();
                }
                else
                {
                    Activated = true;
                    State = WindowState.ACTIVATED;
                    result.SetResult();
                }
            }
            catch (Exception e)
            {
                result.SetException(e);
            }

            return result;
        }

        /// <summary>
        ///     Passivate
        /// </summary>
        /// <returns></returns>
        public virtual IAsyncResult Passivate(bool ignoreAnimation)
        {
            AsyncResult result = new AsyncResult();
            try
            {
                if (!Visibility)
                {
                    result.SetException(new InvalidOperationException("The window is not visible."));
                    return result;
                }

                if (!Activated)
                {
                    result.SetResult();
                    return result;
                }

                Activated = false;
                State = WindowState.PASSIVATED;

                if (!ignoreAnimation && PassivationAnimation != null)
                    PassivationAnimation.OnStart(() => { State = WindowState.PASSIVATION_ANIMATION_BEGIN; }).OnEnd(() =>
                    {
                        State = WindowState.PASSIVATION_ANIMATION_END;
                        result.SetResult();
                    }).Play();
                else
                    result.SetResult();
            }
            catch (Exception e)
            {
                result.SetException(e);
            }

            return result;
        }

        public virtual IAsyncResult DoShow(bool ignoreAnimation = false)
        {
            AsyncResult result = new AsyncResult();
            Action<IPromise> action = promise =>
            {
                try
                {
                    if (!Created)
                        Create();

                    OnShow();
                    Visibility = true;
                    State = WindowState.VISIBLE;
                    if (!ignoreAnimation && EnterAnimation != null)
                        EnterAnimation.OnStart(() => { State = WindowState.ENTER_ANIMATION_BEGIN; }).OnEnd(() =>
                        {
                            State = WindowState.ENTER_ANIMATION_END;
                            promise.SetResult();
                        }).Play();
                    else
                        promise.SetResult();
                }
                catch (Exception e)
                {
                    promise.SetException(e);

                    if (log.IsWarnEnabled)
                        log.WarnFormat("The window named \"{0}\" failed to open!Error:{1}", Name, e);
                }
            };
            action(result);
            return result;
        }

        public virtual IAsyncResult DoHide(bool ignoreAnimation = false)
        {
            AsyncResult result = new AsyncResult();
            Action<IPromise> action = promise =>
            {
                try
                {
                    if (!ignoreAnimation && ExitAnimation != null)
                    {
                        ExitAnimation.OnStart(() => { State = WindowState.EXIT_ANIMATION_BEGIN; }).OnEnd(() =>
                        {
                            State = WindowState.EXIT_ANIMATION_END;
                            Visibility = false;
                            State = WindowState.INVISIBLE;
                            OnHide();
                            promise.SetResult();
                        }).Play();
                    }
                    else
                    {
                        Visibility = false;
                        State = WindowState.INVISIBLE;
                        OnHide();
                        promise.SetResult();
                    }
                }
                catch (Exception e)
                {
                    promise.SetException(e);

                    if (log.IsWarnEnabled)
                        log.WarnFormat("The window named \"{0}\" failed to hide!Error:{1}", Name, e);
                }
            };
            action(result);
            return result;
        }

        public virtual void DoDismiss()
        {
            try
            {
                if (!Dismissed)
                {
                    State = WindowState.DISMISS_BEGIN;
                    Dismissed = true;
                    OnDismiss();
                    RaiseOnDismissed();
                    WindowManager.Remove(this);

                    if (!IsDestroyed() && gameObject != null)
                        Destroy(gameObject);
                    State = WindowState.DISMISS_END;
                    dismissTransition = null;
                }
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("The window named \"{0}\" failed to dismiss!Error:{1}", Name, e);
            }
        }

        public event EventHandler ActivatedChanged
        {
            add
            {
                lock (_lock)
                {
                    activatedChanged += value;
                }
            }
            remove
            {
                lock (_lock)
                {
                    activatedChanged -= value;
                }
            }
        }

        public TProps Properties
        {
            get
            {
                return properties;
            }
            set
            {
                if (value != null)
                {
                    SetProperties(value);
                }
            }
        }

        public event EventHandler VisibilityChanged
        {
            add
            {
                lock (_lock)
                {
                    visibilityChanged += value;
                }
            }
            remove
            {
                lock (_lock)
                {
                    visibilityChanged -= value;
                }
            }
        }

        public event EventHandler OnDismissed
        {
            add
            {
                lock (_lock)
                {
                    onDismissed += value;
                }
            }
            remove
            {
                lock (_lock)
                {
                    onDismissed -= value;
                }
            }
        }

        public event EventHandler<WindowStateEventArgs> StateChanged
        {
            add
            {
                lock (_lock)
                {
                    stateChanged += value;
                }
            }
            remove
            {
                lock (_lock)
                {
                    stateChanged -= value;
                }
            }
        }

        public IWindowManager WindowManager
        {
            get => windowManager ?? (windowManager = FindObjectOfType<GlobalWindowManager>());
            set => windowManager = value;
        }
        
        public GlobalWindowManager CurrentGlobalWindowManager
        {
            get => globalWindowManager ?? (globalWindowManager = FindObjectOfType<GlobalWindowManager>());
            set => globalWindowManager = value;
        }

        public bool Created { get; private set; }

        public bool Dismissed { get; private set; }

        public bool Activated
        {
            get => activated;
            protected set
            {
                if (activated == value)
                    return;

                activated = value;
                OnActivatedChanged();
                RaiseActivatedChanged();
            }
        }

        public WindowType WindowType
        {
            get => windowType;
            set => windowType = value;
        }

        public int WindowPriority
        {
            get => windowPriority;
            set
            {
                if (value < 0)
                    windowPriority = 0;
                else if (value > 10)
                    windowPriority = 10;
                else
                    windowPriority = value;
            }
        }

        public void Create(IBundle bundle = null)
        {
            if (dismissTransition != null || Dismissed)
                throw new ObjectDisposedException(Name);

            if (Created)
                return;

            State = WindowState.CREATE_BEGIN;
            Visibility = false;
            Interactable = Activated;
            OnCreate(bundle);
            WindowManager.Add(this);
            Created = true;
            State = WindowState.CREATE_END;
        }

        public ITransition Show(IScreenProperties props, bool ignoreAnimation = false)
        {
            if (dismissTransition != null || Dismissed)
                throw new InvalidOperationException("The window has been destroyed");

            if (Visibility)
                throw new InvalidOperationException("The window is already visible.");
            
            if (props != null)
                SetProperties(props);
            
            return WindowManager.Show(this).DisableAnimation(ignoreAnimation);
        }

        public ITransition Hide(bool ignoreAnimation = false)
        {
            if (!Created)
                throw new InvalidOperationException("The window has not been created.");

            if (Dismissed)
                throw new InvalidOperationException("The window has been destroyed.");

            if (!Visibility)
                throw new InvalidOperationException("The window is not visible.");

            return WindowManager.Hide(this).DisableAnimation(ignoreAnimation);
        }

        public ITransition Dismiss(bool ignoreAnimation = false)
        {
            if (dismissTransition != null)
                return dismissTransition;

            if (Dismissed)
                throw new InvalidOperationException(string.Format("The window[{0}] has been destroyed.", Name));

            dismissTransition = WindowManager.Dismiss(this).DisableAnimation(ignoreAnimation);
            return dismissTransition;
        }

        /// <summary>
        /// When setting the properties, this method is called.
        /// This way, you can extend the usage of your properties by
        /// certain conditions.
        /// </summary>
        /// <param name="props">Properties.</param>
        public void SetProperties(IScreenProperties props)
        {
            if (props != null)
            {
                if (props is TProps windowProperties)
                {
                    properties = windowProperties;
                    OnPropertiesSet();
                }
            }
        }

        protected void RaiseActivatedChanged()
        {
            try
            {
                if (activatedChanged != null)
                    activatedChanged(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("{0}", e);
            }
        }

        protected void RaiseVisibilityChanged()
        {
            try
            {
                if (visibilityChanged != null)
                    visibilityChanged(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("{0}", e);
            }
        }

        protected void RaiseOnDismissed()
        {
            try
            {
                if (onDismissed != null)
                    onDismissed(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("{0}", e);
            }
        }

        protected void RaiseStateChanged(WindowState windowState)
        {
            try
            {
                stateChanged?.Invoke(this, new WindowStateEventArgs(windowState));
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.WarnFormat("{0}", e);
            }
        }

        protected virtual void OnActivatedChanged()
        {
            Interactable = Activated;
        }

        protected abstract void OnCreate(IBundle bundle);

        /// <summary>
        ///     Called before the start of the display animation.
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        ///     Called at the end of the hidden animation.
        /// </summary>
        protected virtual void OnHide()
        {
        }

        protected virtual void OnDismiss()
        {
        }

        /// <summary>
        /// When Properties are set for this screen, this method is called.
        /// At this point, you can safely access Properties.
        /// </summary>
        protected virtual void OnPropertiesSet()
        {
        }
    }
}