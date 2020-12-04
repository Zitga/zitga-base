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
using Loxodon.Framework.Asynchronous;
using Loxodon.Framework.Execution;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Loxodon.Framework.Views
{
    public class DefaultUIViewLocator : UIViewLocatorBase
    {
        private GlobalWindowManager globalWindowManager;
        private readonly Dictionary<string, WeakReference> templates = new Dictionary<string, WeakReference>();

        protected string Normalize(string name)
        {
            var index = name.IndexOf('.');
            if (index < 0)
                return name;

            return name.Substring(0, index);
        }

        protected virtual IWindowManager GetDefaultWindowManager()
        {
            if (globalWindowManager != null)
                return globalWindowManager;

            globalWindowManager = Object.FindObjectOfType<GlobalWindowManager>();
            if (globalWindowManager == null)
                throw new NotFoundException("GlobalWindowManager");

            return globalWindowManager;
        }

        public override T LoadView<T>(string name)
        {
            return DoLoadView<T>(name);
        }

        protected virtual T DoLoadView<T>(string name)
        {
            name = Normalize(name);
            WeakReference weakRef;
            GameObject viewTemplateGo = null;
            try
            {
                if (templates.TryGetValue(name, out weakRef) && weakRef.IsAlive)
                {
                    viewTemplateGo = (GameObject) weakRef.Target;

                    //Check if the object is valid because it may have been destroyed.
                    //Unmanaged objects,the weak caches do not accurately track the validity of objects.
                    if (viewTemplateGo != null)
                    {
                        var goName = viewTemplateGo.name;
                    }
                }
            }
            catch (Exception)
            {
                viewTemplateGo = null;
            }

            if (viewTemplateGo == null)
            {
                viewTemplateGo = Resources.Load<GameObject>(name);
                if (viewTemplateGo != null)
                {
                    viewTemplateGo.SetActive(false);
                    templates[name] = new WeakReference(viewTemplateGo);
                }
            }

            if (viewTemplateGo == null || viewTemplateGo.GetComponent<T>() == null)
                return default;

            GameObject go = Object.Instantiate(viewTemplateGo);
            go.name = viewTemplateGo.name;
            T view = go.GetComponent<T>();
            if (view == null && go != null)
                Object.Destroy(go);
            return view;
        }

        public override IProgressResult<float, T> LoadViewAsync<T>(string name)
        {
            var result = new ProgressResult<float, T>();
            Executors.RunOnCoroutineNoReturn(DoLoad(result, name));
            return result;
        }

        protected virtual IEnumerator DoLoad<T>(IProgressPromise<float, T> promise, string name,
            IWindowManager windowManager = null)
        {
            name = Normalize(name);
            WeakReference weakRef;
            GameObject viewTemplateGo = null;
            try
            {
                if (templates.TryGetValue(name, out weakRef) && weakRef.IsAlive)
                {
                    viewTemplateGo = (GameObject) weakRef.Target;

                    //Check if the object is valid because it may have been destroyed.
                    //Unmanaged objects,the weak caches do not accurately track the validity of objects.
                    if (viewTemplateGo != null)
                    {
                        var goName = viewTemplateGo.name;
                    }
                }
            }
            catch (Exception)
            {
                viewTemplateGo = null;
            }

            if (viewTemplateGo == null)
            {
                ResourceRequest request = Resources.LoadAsync<GameObject>(name);
                while (!request.isDone)
                {
                    promise.UpdateProgress(request.progress);
                    yield return null;
                }

                viewTemplateGo = (GameObject) request.asset;
                if (viewTemplateGo != null)
                {
                    viewTemplateGo.SetActive(false);
                    templates[name] = new WeakReference(viewTemplateGo);
                }
            }

            if (viewTemplateGo == null || viewTemplateGo.GetComponent<T>() == null)
            {
                promise.UpdateProgress(1f);
                promise.SetException(new NotFoundException(name));
                yield break;
            }

            GameObject go = Object.Instantiate(viewTemplateGo);
            go.name = viewTemplateGo.name;
            T view = go.GetComponent<T>();
            if (view == null)
            {
                Object.Destroy(go);
                promise.SetException(new NotFoundException(name));
            }
            else
            {
                if (windowManager != null && view is IWindow)
                    (view as IWindow).WindowManager = windowManager;

                promise.UpdateProgress(1f);
                promise.SetResult(view);
            }
        }

        public override T LoadWindow<T>(string name)
        {
            return LoadWindow<T>(null, name);
        }

        public override T LoadWindow<T>(IWindowManager windowManager, string name)
        {
            if (windowManager == null)
                windowManager = GetDefaultWindowManager();

            T target = DoLoadView<T>(name);
            if (target != null)
                target.WindowManager = windowManager;

            return target;
        }

        public override IProgressResult<float, T> LoadWindowAsync<T>(string name)
        {
            return LoadWindowAsync<T>(null, name);
        }

        public override IProgressResult<float, T> LoadWindowAsync<T>(IWindowManager windowManager, string name)
        {
            if (windowManager == null)
                windowManager = GetDefaultWindowManager();

            var result = new ProgressResult<float, T>();
            Executors.RunOnCoroutineNoReturn(DoLoad(result, name, windowManager));
            return result;
        }
    }
}