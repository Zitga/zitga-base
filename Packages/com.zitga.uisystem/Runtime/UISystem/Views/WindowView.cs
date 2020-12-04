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
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
 * SOFTWARE.
 */

using Loxodon.Framework.Views.Animations;
using UnityEngine;

namespace Loxodon.Framework.Views
{
    public class WindowView : UIView, IWindowView
    {
        public virtual IAnimation ActivationAnimation { get; set; }

        public virtual IAnimation PassivationAnimation { get; set; }

        public virtual void AddView(IUIView view, bool worldPositionStays = false)
        {
            if (view == null)
                return;

            Transform t = view.Transform;
            if (t == null || t.parent == transform)
                return;

            view.Owner.layer = gameObject.layer;
            t.SetParent(transform, worldPositionStays);
        }

        public virtual void AddView(IUIView view, UILayout layout)
        {
            if (view == null)
                return;

            Transform t = view.Transform;
            if (t == null)
                return;

            if (t.parent == transform)
            {
                layout?.Invoke(view.RectTransform);
                return;
            }

            view.Owner.layer = gameObject.layer;
            t.SetParent(transform, false);
            layout?.Invoke(view.RectTransform);
        }

        public virtual void RemoveView(IUIView view, bool worldPositionStays = false)
        {
            if (view == null)
                return;

            Transform t = view.Transform;
            if (t == null || t.parent != transform)
                return;

            t.SetParent(null, worldPositionStays);
        }
    }
}