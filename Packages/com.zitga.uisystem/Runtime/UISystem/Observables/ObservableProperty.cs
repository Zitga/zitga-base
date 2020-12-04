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

namespace Loxodon.Framework.Observables
{
    [Serializable]
    public class ObservablePropertyBase<T>
    {
        private readonly object _lock = new object();
        protected T _value;
        private EventHandler valueChanged;

        public ObservablePropertyBase() : this(default)
        {
        }

        public ObservablePropertyBase(T value)
        {
            _value = value;
        }

        public virtual Type Type => typeof(T);

        public event EventHandler ValueChanged
        {
            add
            {
                lock (_lock)
                {
                    valueChanged += value;
                }
            }
            remove
            {
                lock (_lock)
                {
                    valueChanged -= value;
                }
            }
        }

        protected void RaiseValueChanged()
        {
            EventHandler handler = valueChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected virtual bool Equals(T x, T y)
        {
            if (x != null)
            {
                if (y != null)
                    return x.Equals(y);
                return false;
            }

            if (y != null)
                return false;

            return true;
        }
    }

    [Serializable]
    public class ObservableProperty : ObservablePropertyBase<object>, IObservableProperty
    {
        public ObservableProperty() : this(null)
        {
        }

        public ObservableProperty(object value) : base(value)
        {
        }

        public override Type Type => _value != null ? _value.GetType() : typeof(object);

        public virtual object Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value))
                    return;

                _value = value;
                RaiseValueChanged();
            }
        }

        public override string ToString()
        {
            object v = Value;
            if (v == null)
                return "";

            return v.ToString();
        }
    }

    [Serializable]
    public class ObservableProperty<T> : ObservablePropertyBase<T>, IObservableProperty<T>
    {
        public ObservableProperty() : this(default)
        {
        }

        public ObservableProperty(T value) : base(value)
        {
        }

        public virtual T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value))
                    return;

                _value = value;
                RaiseValueChanged();
            }
        }

        object IObservableProperty.Value
        {
            get => Value;
            set => Value = (T) value;
        }

        public override string ToString()
        {
            T v = Value;
            if (v == null)
                return "";

            return v.ToString();
        }

        public static implicit operator T(ObservableProperty<T> data)
        {
            return data.Value;
        }

        public static implicit operator ObservableProperty<T>(T data)
        {
            return new ObservableProperty<T>(data);
        }
    }
}