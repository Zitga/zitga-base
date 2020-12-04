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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Loxodon.Framework.Contexts;
using Loxodon.Log;
using UnityEngine;

namespace Loxodon.Framework.Views
{
    /// <summary>
    /// Contain properties need to show a window
    /// </summary>
    class WindowOpenProperties
    {
        // Id of the window: Path to load window
        public string Id { get; private set; }

        // Data when need to show window
        public IScreenProperties Properties { get; private set; }

        public WindowOpenProperties(string id, IScreenProperties properties)
        {
            Id = id;
            Properties = properties;
        }
    }

    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public class GlobalWindowManager : WindowManager, IUpdateSystem
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GlobalWindowManager));

        /// <summary>
        /// The context of the game. Use to get game services
        /// </summary>
        private ApplicationContext context;

        /// <summary>
        /// The window which loading, creating, showing with animation but not finish.
        /// Add when call to show
        /// Remove when showing animation finish
        /// </summary>
        private Queue<WindowOpenProperties> loadingWindow;

        /// <summary>
        /// All showing windows in the game. Just contain window after finish animations.
        /// </summary> 
        private List<IWindow> showingWindow;
        
        /// <summary>
        /// All showing windows in the game. Just contain window after finish animations.
        /// </summary> 
        private List<IWindow> hidingWindow;

        /// <summary>
        /// All loaded windows in the game. It contains all showing window collection
        /// </summary>
        private Dictionary<string, IWindow> loadedWindow;

        private void Awake()
        {
            context = Context.GetApplicationContext();

            loadingWindow = new Queue<WindowOpenProperties>();

            showingWindow = new List<IWindow>();
            
            hidingWindow = new List<IWindow>();

            loadedWindow = new Dictionary<string, IWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            context.GetService<GlobalUpdateSystem>().Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            context.GetService<GlobalUpdateSystem>().Remove(this);
        }

        /// <summary>
        /// Opens the Window with the given id, passing in Properties.
        /// </summary>
        /// <param name="windowId">Identifier.</param>
        /// <param name="properties">Properties.</param>
        /// <seealso cref="AWindowProperties"/>
        public void OpenWindow(string windowId, IScreenProperties properties)
        {
            StartCoroutine(ShowWindowById(windowId, properties));
        }

        /// <summary>
        /// Closes the Window with the given Id.
        /// </summary>
        /// <param name="window">Identifier.</param>
        public void CloseWindow(IWindow window)
        {
            StartCoroutine(HideWindow(window));
        }

        /// <summary>
        /// Closes the Window with the given Id.
        /// </summary>
        /// <param name="windowId">Identifier.</param>
        public void CloseWindow(string windowId)
        {
            StartCoroutine(HideWindowById(windowId));
        }

        public IEnumerator ShowWindowById(string windowId)
        {
            yield return ShowWindowById(windowId, null);
        }

        public IEnumerator ShowWindowById(string windowId, IScreenProperties properties)
        {
            IWindow window;

            if (IsWindowLoading(windowId))
            {
                // may be can update model view after that
                yield break;
            }

            if (IsWindowLoaded(windowId))
            {
                loadedWindow.TryGetValue(windowId, out window);
            }
            else
            {
                loadingWindow.Enqueue(new WindowOpenProperties(windowId, properties));

                var locator = context.GetService<IUIViewLocator>();

                var result = locator.LoadWindowAsync<IWindow>(windowId);

                while (!result.IsDone)
                {
                    Debug.LogFormat("Percentage: {0}% ", result.Progress);
                    yield return null;
                }

                Debug.LogFormat("IsDone:{0} Result:{1}", result.IsDone, result.Result);

                window = result.Result;

                window.Create();

                var lastWindowOpenProperties = loadingWindow.Dequeue();

                if (lastWindowOpenProperties.Id != windowId)
                {
                    log.ErrorFormat("Window is valid: {0} != {1}", lastWindowOpenProperties.Id, windowId);
                    yield break;
                }

                loadedWindow.Add(windowId, window);
            }

            if (window != null && window.Visibility == false)
            {
                showingWindow.Add(window);

                ITransition transition = window.Show(properties).OnStateChanged((w, state) =>
                {
                    // log.DebugFormat("Window Show:{0} State{1}", w.Name, state);
                });

                yield return transition.WaitForDone();
            }

            if (loadingWindow.Count > 0)
            {
                var nextWindowOpenProperties = loadingWindow.Peek();
                yield return ShowWindowById(nextWindowOpenProperties.Id, nextWindowOpenProperties.Properties);
            }
        }

        public IEnumerator HideWindow(IWindow window)
        {
            if (hidingWindow.Contains(window))
            {
                log.WarnFormat("Window was hiding: {0}", nameof(window));
                yield break;
            }
            
            if (showingWindow.Contains(window) && window.Visibility)
            {
                hidingWindow.Add(window);
                
                ITransition transition = window.Hide().OnStateChanged((w, state) =>
                {
                    // log.DebugFormat("Window Hide:{0} State{1}", w.Name, state);
                });

                yield return transition.WaitForDone();

                showingWindow.Remove(window);
                hidingWindow.Remove(window);
            }
            else
            {
                log.WarnFormat("Window has already hid: {0}", nameof(window));
            }
        }

        public IEnumerator HideWindowById(string windowId)
        {
            loadedWindow.TryGetValue(windowId, out IWindow window);
            if (window != null)
            {
                yield return HideWindow(window);
            }
            else
            {
                log.ErrorFormat("Window is not exist: {0}", windowId);
            }
        }

        protected void CloseTopPopup()
        {
            if (Current == null)
            {
                log.Error("No available window to close");
            }
            else
            {
                // if (Current as LaunchWindow)
                // {
                //     log.Warn("Do something! Can't close this popup");
                // }
                // else
                // {
                //     StartCoroutine(HideWindow(Current));
                // }
            }
        }

        /// <summary>
        /// Is window loading or in queue to load?
        /// </summary>
        /// <param name="windowId"></param>
        /// <returns></returns>
        private bool IsWindowLoading(string windowId)
        {
            return loadingWindow.Any(x => x.Id.Equals(windowId));
        }

        /// <summary>
        /// Is window called to load?
        /// </summary>
        /// <param name="uiViewId"></param>
        /// <returns></returns>
        private bool IsWindowLoaded(string uiViewId)
        {
            return loadedWindow.ContainsKey(uiViewId);
        }

        public void OnUpdate(float deltaTime)
        {
#if UNITY_ANDROID
            if (Input.GetKey(KeyCode.Escape))
            {
                // log.DebugFormat("Current window: {0}", Current.Name);
                CloseTopPopup();
            }
#endif
        }
    }
}