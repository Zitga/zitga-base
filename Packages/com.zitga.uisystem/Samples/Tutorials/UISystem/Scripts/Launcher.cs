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
using Loxodon.Framework.Views;
using Loxodon.Framework.Contexts;
// using Loxodon.Framework.Localizations;
using Loxodon.Framework.Services;
using Loxodon.Log;
using UnityEngine;
using Zitga.Sound;

namespace Loxodon.Framework.Examples
{
    public class Launcher : MonoBehaviour
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Launcher));

        private ApplicationContext context;

        private GlobalWindowManager windowManager;

        private GlobalUpdateSystem globalUpdateSystem;

        private void Awake()
        {
            context = Context.GetApplicationContext();

            RegisterService();
        }

        private void RegisterService()
        {
            IServiceContainer container = context.GetContainer();

            /* Initialize the ui view locator and register UIViewLocator */
            container.Register<IUIViewLocator>(new ResourcesViewLocator());
            
            /* register IUpdateSystem */
            container.Register(new GlobalUpdateSystem());
           
            /* register IUpdateSystem */
            container.Register(new SoundManager());

            /* register Localization */
            // CultureInfo cultureInfo = Locale.GetCultureInfoByLanguage(SystemLanguage.Vietnamese);
            // Localization.Current.CultureInfo = cultureInfo;
            // container.Register(Localization.Current);

            globalUpdateSystem = context.GetService <GlobalUpdateSystem>();

#if UNITY_EDITOR
            windowManager = FindObjectOfType<GlobalWindowManager>();
            if (windowManager != null)
                throw new NotFoundException("Exist the GlobalWindowManager.");
#endif
            windowManager = GetComponentInChildren<Canvas>().gameObject.AddComponent<GlobalWindowManager>();
            /* register GlobalWindowManager */
            container.Register(windowManager);
        }

        private IEnumerator Start()
        {
            log.Debug("Start");
            yield return windowManager.ShowWindowById(WindowIds.UIStartupWindow);
            log.Debug("End");
        }

        private void Update()
        {
            globalUpdateSystem.OnUpdate(Time.deltaTime);
        }
    }
}