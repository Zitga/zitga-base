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
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zitga.CsvTools;

namespace Zitga.LocalizeTools
{
    public sealed class Localization
    {
        private static readonly object _instanceLock = new object();
        private static Localization _instance;

        private readonly object _lock = new object();
        private CultureInfo cultureInfo;
        private EventHandler cultureInfoChanged;

        private readonly IDataProvider dataProvider;

        public static Localization Current
        {
            get
            {
                if (_instance != null)
                    lock (_instanceLock)
                    {
                        return _instance;
                    }

                lock (_instanceLock)
                {
                    if (_instance == null)
                        _instance = new Localization();
                    return _instance;
                }
            }
            set { lock (_instanceLock) { _instance = value; } }
        }

        private Localization() : this(null)
        {
        }

        private Localization(CultureInfo cultureInfo)
        {
            this.cultureInfo = cultureInfo ?? Locale.GetCultureInfo();

            dataProvider = new ResourceDataProvider(this);
        }

        public event EventHandler CultureInfoChanged
        {
            add { lock (_lock) { cultureInfoChanged += value; } }
            remove { lock (_lock) { cultureInfoChanged -= value; } }
        }

        
        public CultureInfo CultureInfo
        {
            get { return cultureInfo; }
            set
            {
                if (value == null || (cultureInfo != null && cultureInfo.Equals(value)))
                    return;

                dataProvider.ClearData();
                
                cultureInfo = value;
                OnCultureInfoChanged();
            }
        }

        public UniTask<string> Get(string category, string key)
        {
            return dataProvider.Get(category, key);
        }

        private void RaiseCultureInfoChanged()
        {
            try
            {
                Debug.Log("language: " + cultureInfo.Name);
                this.cultureInfoChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnCultureInfoChanged()
        {
            RaiseCultureInfoChanged();
        }

    }
}
