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
using Cysharp.Threading.Tasks;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Views;
using UnityEngine.UI;

namespace Loxodon.Framework.Examples
{
    public class LaunchWindow : Window
    {
        public Text progressBarText;
        public Slider progressBarSlider;
        public Text tipText;
        public Button button;
        private IDisposable subscription;

        protected override void OnCreate(IBundle bundle)
        {
            button.onClick.AddListener(OnClickButton);
        }

        protected override void OnShow()
        {
            Unzip().Forget();
        }

        public override void DoDismiss()
        {
            base.DoDismiss();
            if (subscription == null) return;
            subscription.Dispose();
            subscription = null;
        }

        protected override async UniTaskVoid OnLocalizeChanged()
        {
            // tipText.text = await R.mail.email_sender_name;

            UpdateProcess().Forget();
        }

        private void OnClickButton()
        {
            OnOpenLoginWindow();
        }

        private float progress;
        private async UniTaskVoid Unzip()
        {
            progressBarSlider.gameObject.SetActive(true);
            try
            {
                progress = 0f;
                while (progress < 1f)
                {
                    progress += 0.01f;
                    progressBarSlider.value = progress;
                    UpdateProcess().Forget();
                    await UniTask.WaitForEndOfFrame();
                }
            }
            finally
            {
                progressBarSlider.gameObject.SetActive(false);
                tipText.text = "";
                OnOpenLoginWindow();
            }
        }

        private async UniTaskVoid UpdateProcess()
        {
            // progressBarText.text = $"{await R.mail.email_sender_name} {progress * 100}%";
        }

        private void OnOpenLoginWindow()
        {
            CurrentGlobalWindowManager.OpenWindow(WindowIds.UILoginWindow, new LoginWindowProperties());
        }
    }
}