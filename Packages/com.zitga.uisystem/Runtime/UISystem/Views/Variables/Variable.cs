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
using Object = UnityEngine.Object;

namespace Loxodon.Framework.Views.Variables
{
    [Serializable]
    public enum VariableType
    {
        Object,
        GameObject,
        Component,
        Boolean,
        Integer,
        Float,
        String,
        Color,
        Vector2,
        Vector3,
        Vector4
    }

    [Serializable]
    public class Variable
    {
        [SerializeField] protected string name = "";

        [SerializeField] protected Object objectValue;

        [SerializeField] protected string dataValue;

        [SerializeField] protected VariableType variableType;

        public virtual string Name
        {
            get => name;
            set => name = value;
        }

        public virtual VariableType VariableType => variableType;

        public virtual Type ValueType
        {
            get
            {
                switch (variableType)
                {
                    case VariableType.Boolean:
                        return typeof(bool);
                    case VariableType.Float:
                        return typeof(float);
                    case VariableType.Integer:
                        return typeof(int);
                    case VariableType.String:
                        return typeof(string);
                    case VariableType.Color:
                        return typeof(Color);
                    case VariableType.Vector2:
                        return typeof(Vector2);
                    case VariableType.Vector3:
                        return typeof(Vector3);
                    case VariableType.Vector4:
                        return typeof(Vector4);
                    case VariableType.Object:
                        return objectValue == null ? typeof(Object) : objectValue.GetType();
                    case VariableType.GameObject:
                        return objectValue == null ? typeof(GameObject) : objectValue.GetType();
                    case VariableType.Component:
                        return objectValue == null ? typeof(Component) : objectValue.GetType();
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public virtual void SetValue<T>(T value)
        {
            SetValue(value);
        }

        public virtual T GetValue<T>()
        {
            return (T) GetValue();
        }

        public virtual void SetValue(object value)
        {
            switch (variableType)
            {
                case VariableType.Boolean:
                    dataValue = DataConverter.GetString((bool) value);
                    break;
                case VariableType.Float:
                    dataValue = DataConverter.GetString((float) value);
                    break;
                case VariableType.Integer:
                    dataValue = DataConverter.GetString((int) value);
                    break;
                case VariableType.String:
                    dataValue = DataConverter.GetString((string) value);
                    break;
                case VariableType.Color:
                    dataValue = DataConverter.GetString((Color) value);
                    break;
                case VariableType.Vector2:
                    dataValue = DataConverter.GetString((Vector2) value);
                    break;
                case VariableType.Vector3:
                    dataValue = DataConverter.GetString((Vector3) value);
                    break;
                case VariableType.Vector4:
                    dataValue = DataConverter.GetString((Vector4) value);
                    break;
                case VariableType.Object:
                    objectValue = (Object) value;
                    break;
                case VariableType.GameObject:
                    objectValue = (GameObject) value;
                    break;
                case VariableType.Component:
                    objectValue = (Component) value;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public virtual object GetValue()
        {
            switch (variableType)
            {
                case VariableType.Boolean:
                    return DataConverter.ToBoolean(dataValue);
                case VariableType.Float:
                    return DataConverter.ToSingle(dataValue);
                case VariableType.Integer:
                    return DataConverter.ToInt32(dataValue);
                case VariableType.String:
                    return DataConverter.ToString(dataValue);
                case VariableType.Color:
                    return DataConverter.ToColor(dataValue);
                case VariableType.Vector2:
                    return DataConverter.ToVector2(dataValue);
                case VariableType.Vector3:
                    return DataConverter.ToVector3(dataValue);
                case VariableType.Vector4:
                    return DataConverter.ToVector4(dataValue);
                case VariableType.Object:
                    return objectValue;
                case VariableType.GameObject:
                    return objectValue;
                case VariableType.Component:
                    return objectValue;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}