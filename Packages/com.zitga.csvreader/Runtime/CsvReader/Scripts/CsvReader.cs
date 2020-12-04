using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace Zitga.CsvTools
{
    public static class CsvReader
    {
        public static T[] Deserialize<T>(string text, char separator = ',')
        {
            return (T[]) CreateArray(typeof(T), ParseCsv(text, separator));
        }

        public static T[] Deserialize<T>(List<string[]> rows)
        {
            return (T[]) CreateArray(typeof(T), rows);
        }

        public static T DeserializeIdValue<T>(string text, int id_col = 0, int value_col = 1)
        {
            return (T) CreateIdValue(typeof(T), ParseCsv(text), id_col, value_col);
        }

        public static T DeserializeIdValue<T>(List<string[]> rows, int id_col = 0, int value_col = 1)
        {
            return (T) CreateIdValue(typeof(T), rows, id_col, value_col);
        }


        private static object CreateArray(Type type, List<string[]> rows)
        {
            var (countElement, startRows) = CountNumberElement(1, 0, 0, rows);
            Array arrayValue = Array.CreateInstance(type, countElement);
            Dictionary<string, int> table = new Dictionary<string, int>();

            for (int i = 0; i < rows[0].Length; i++)
            {
                string id = rows[0][i];
                if (IsValidKeyFormat(id))
                {
                    var camelId = ConvertSnakeCaseToCamelCase(id);
                    
                    if (!table.ContainsKey(camelId))
                    {
                        table.Add(camelId, i);
                    }
                    else
                    {
                        throw new Exception("Key is duplicate: " + id);
                    }
                }
                else
                {
                    throw new Exception("Key is not valid: " + id);
                }
            }

            for (int i = 0; i < arrayValue.Length; i++)
            {
                object rowData = Create(startRows[i], 0, rows, table, type);
                arrayValue.SetValue(rowData, i);
            }

            return arrayValue;
        }

        static object Create(int index, int parentIndex, List<string[]> rows, Dictionary<string, int> table, Type type)
        {
            object v = Activator.CreateInstance(type);

            FieldInfo[] fieldInfo = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var cols = rows[index];
            foreach (FieldInfo tmp in fieldInfo)
            {
                bool isPrimitive = IsPrimitive(tmp);
                if (isPrimitive)
                {
                    if (table.ContainsKey(tmp.Name))
                    {
                        int idx = table[tmp.Name];
                        if (idx < cols.Length)
                            SetValue(v, tmp, cols[idx]);
                    }
                    else
                    {
                        throw new Exception("Key is not exist: " + tmp.Name);
                    }
                }
                else
                {
                    if (tmp.FieldType.IsArray)
                    {
                        var elementType = GetElementTypeFromFieldInfo(tmp);

                        var objectIndex = GetObjectIndex(elementType, table);

                        var (countElement, startRows) = CountNumberElement(index, objectIndex, parentIndex, rows);

                        Array arrayValue = Array.CreateInstance(elementType, countElement);

                        for (int i = 0; i < arrayValue.Length; i++)
                        {
                            var value = Create(startRows[i], objectIndex, rows, table, elementType);
                            arrayValue.SetValue(value, i);
                        }

                        tmp.SetValue(v, arrayValue);
                    }
                    else
                    {
                        var typeName = tmp.FieldType.FullName;
                        if (typeName == null)
                        {
                            throw new Exception("Full name is nil");
                        }
                    
                        Type elementType = GetType(typeName);

                        var objectIndex = GetObjectIndex(elementType, table);

                        var value = Create(index, objectIndex, rows, table, elementType);

                        tmp.SetValue(v, value);
                    }
                }
            }

            return v;
        }

        static void SetValue(object v, FieldInfo fieldInfo, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                var type = fieldInfo.FieldType;
                if (type == typeof(string))
                {
                    value = string.Empty;
                }else if (type == typeof(int) || type == typeof(float) || type == typeof(double))
                {
                    value = "0";
                }else if (type == typeof(bool))
                {
                    value = "FALSE";
                }
                else
                {
                    throw new Exception("value is nil");
                }
            }

            if (fieldInfo.FieldType.IsArray)
            {
                Type elementType = fieldInfo.FieldType.GetElementType();
                string[] elem = value.Split(',', '~');
                Array arrayValue = Array.CreateInstance(elementType ?? throw new InvalidOperationException(), elem.Length);
                for (int i = 0; i < elem.Length; i++)
                {
                    if (elementType == typeof(string))
                        arrayValue.SetValue(elem[i], i);
                    else
                        arrayValue.SetValue(Convert.ChangeType(elem[i], elementType), i);
                }

                fieldInfo.SetValue(v, arrayValue);
            }
            else if (fieldInfo.FieldType.IsEnum)
                fieldInfo.SetValue(v, Enum.Parse(fieldInfo.FieldType, value));
            else if (value.IndexOf('.') != -1 &&
                     (fieldInfo.FieldType == typeof(Int32) || fieldInfo.FieldType == typeof(Int64) ||
                      fieldInfo.FieldType == typeof(Int16)))
            {
                float f = (float) Convert.ChangeType(value, typeof(float));
                fieldInfo.SetValue(v, Convert.ChangeType(f, fieldInfo.FieldType));
            }
            else if (fieldInfo.FieldType == typeof(string))
                fieldInfo.SetValue(v, value);
            else
            {
                fieldInfo.SetValue(v, Convert.ChangeType(value, fieldInfo.FieldType));
            }
        }

        static object CreateIdValue(Type type, List<string[]> rows, int idCol = 0, int valCol = 1)
        {
            object v = Activator.CreateInstance(type);

            Dictionary<string, int> table = new Dictionary<string, int>();

            for (int i = 1; i < rows.Count; i++)
            {
                if (rows[i][idCol].Length > 0)
                    table.Add(rows[i][idCol].TrimEnd(' '), i);
            }

            FieldInfo[] fieldInfo = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo tmp in fieldInfo)
            {
                if (table.ContainsKey(tmp.Name))
                {
                    int idx = table[tmp.Name];
                    if (rows[idx].Length > valCol)
                        SetValue(v, tmp, rows[idx][valCol]);
                }
                else
                {
                    Debug.Log("Miss " + tmp.Name);
                }
            }

            return v;
        }

        public static List<string[]> ParseCsv(string text, char separator = ',')
        {
            List<string[]> lines = new List<string[]>();
            List<string> line = new List<string>();
            StringBuilder token = new StringBuilder();
            bool quotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                if (quotes)
                {
                    if ((text[i] == '\\' && i + 1 < text.Length && text[i + 1] == '\"') ||
                        (text[i] == '\"' && i + 1 < text.Length && text[i + 1] == '\"'))
                    {
                        token.Append('\"');
                        i++;
                    }
                    else switch (text[i])
                    {
                        case '\\' when i + 1 < text.Length && text[i + 1] == 'n':
                            token.Append('\n');
                            i++;
                            break;
                        case '\"':
                        {
                            line.Add(token.ToString());
                            token = new StringBuilder();
                            quotes = false;
                            if (i + 1 < text.Length && text[i + 1] == separator)
                                i++;
                            break;
                        }
                        default:
                            token.Append(text[i]);
                            break;
                    }
                }
                else if (text[i] == '\r' || text[i] == '\n')
                {
                    if (token.Length > 0)
                    {
                        line.Add(token.ToString());
                        token = new StringBuilder();
                    }

                    if (line.Count > 0)
                    {
                        lines.Add(line.ToArray());
                        line.Clear();
                    }
                }
                else if (text[i] == separator)
                {
                    line.Add(token.ToString());
                    token = new StringBuilder();
                }
                else if (text[i] == '\"')
                {
                    quotes = true;
                }
                else
                {
                    token.Append(text[i]);
                }
            }

            if (token.Length > 0)
            {
                line.Add(token.ToString());
            }

            if (line.Count > 0)
            {
                lines.Add(line.ToArray());
            }

            return lines;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strFullyQualifiedName"></param>
        /// <returns></returns>
        private static Type GetType(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(strFullyQualifiedName);
                    if (type != null)
                        break;
                }
            }

            if (type == null)
            {
                throw new Exception("Type is null: " + strFullyQualifiedName);
            }

            return type;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        private static int GetObjectIndex(Type type, Dictionary<string, int> table)
        {
            int minIndex = int.MaxValue;
            FieldInfo[] fieldInfo = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo tmp in fieldInfo)
            {
                if (table.ContainsKey(tmp.Name))
                {
                    int idx = table[tmp.Name];
                    if (idx < minIndex)
                        minIndex = idx;
                }
                else
                {
                    //Debug.Log("Miss " + tmp.Name);
                }
            }

            return minIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="objectIndex"></param>
        /// <param name="parentIndex"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        private static (int, List<int>) CountNumberElement(int rowIndex, int objectIndex, int parentIndex,
            List<string[]> rows)
        {
            int count = 0;
            var startRows = new List<int>();

            for (int i = rowIndex; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row[objectIndex].Equals(string.Empty) == false)
                {
                    if (objectIndex == parentIndex)
                    {
                        count++;
                        startRows.Add(i);
                    }
                    else if (row[parentIndex].Equals(string.Empty) || i == rowIndex)
                    {
                        count++;
                        startRows.Add(i);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return (count, startRows);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool IsValidKeyFormat(string key)
        {
            return key.Equals(key.ToLower());
        }

        /// <summary>
        /// Use to check variable and array variables is Primitive or not.
        /// Can't use IsClass or IsPrimitive because Array is always a class.
        /// Want to check the real type of element in array
        /// </summary>
        /// <param name="tmp"></param>
        /// <returns></returns>
        private static bool IsPrimitive(FieldInfo tmp)
        {
            Type type;
            if (tmp.FieldType.IsArray)
            {
                type = GetElementTypeFromFieldInfo(tmp);
            }
            else
            {
                type = tmp.FieldType;
            }

            return IsPrimitive(type);
        }

        private static bool IsPrimitive(Type type)
        {
            return type == typeof(String) || type.IsEnum || type.IsPrimitive;
        }

        private static Type GetElementTypeFromFieldInfo(FieldInfo tmp)
        {
            string fullName = string.Empty;
            if (tmp.FieldType.IsArray)
            {
                if (tmp.FieldType.FullName != null)
                    fullName = tmp.FieldType.FullName.Substring(0, tmp.FieldType.FullName.Length - 2);
            }
            else
            {
                fullName = tmp.FieldType.FullName;
            }

            return GetType(fullName);
        }

        private static string ConvertSnakeCaseToCamelCase(string snakeCase)
        {
            var strings = snakeCase.Split(new[] {"_"}, StringSplitOptions.RemoveEmptyEntries);
            var result = strings[0];
            for (int i = 1; i < strings.Length; i++)
            {
                var currentString = strings[i];
                result += char.ToUpperInvariant(currentString[0]) + currentString.Substring(1, currentString.Length - 1);
            }

            return result;
        }
    }
}