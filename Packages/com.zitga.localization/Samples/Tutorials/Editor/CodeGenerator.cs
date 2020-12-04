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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Zitga.CsvTools;

namespace Localization.Samples.Localization.Editor
{
    public class CodeGenerator
	{
		public const string SearchKey = "Localization/en";
		private const string ScriptPath = "Scripts/Gen/";
		private const string DefaultClassName = "R";
		string template = "using Cysharp.Threading.Tasks;\r\nusing Loxodon.Framework.Localizations;\r\n${namespaces}\r\npublic static class ${name}\r\n{\r\n${properties}}";
		string subTemplate = "\r\n    public static class ${name}\r\n    {\r\n${properties}    }\r\n";
		string format = "        public static UniTask<string> {0} => Localization.Current.Get(\"{1}\", \"{2}\");";

		public string Generate (Dictionary<string, List<string>> dict)
		{
			StringBuilder propertiesBuf = new StringBuilder ();
			StringBuilder namespacesBuf = new StringBuilder (); 

			foreach (var kv in dict)
			{
				propertiesBuf.Append(SetUpClass(kv.Key, kv.Value));
			}

			var code = template.Replace ("${namespaces}", namespacesBuf.ToString ());
			code = code.Replace ("${name}", DefaultClassName);
			code = code.Replace ("${properties}", propertiesBuf.ToString ());
			return code;
		}

		private string SetUpClass(string className, List<string> keys)
		{
			StringBuilder propertiesBuf = new StringBuilder ();
			foreach (var key in keys) {
				propertiesBuf.AppendFormat (format, GetPropertyName (key), className, key).Append (Environment.NewLine).Append (Environment.NewLine);
			}

			var code = subTemplate.Replace("${name}", className);
			code = code.Replace ("${properties}", propertiesBuf.ToString ());
			return code;
		}

		private string GetPropertyName (string key)
		{
			return Regex.Replace (key, "[.]", "_");
		}

		private Dictionary<string, List<string>> CollectKeys(string path)
		{
			var allFiles = Resources.LoadAll<TextAsset>(SearchKey);
		    
			var keyDict = new Dictionary<string, List<string>>();

			foreach (var file in allFiles)
			{
				var content = CsvReader.ParseCsv(file.text, '~');
				if (content.Count == 0)
				{
					Debug.LogErrorFormat("Content is nil: {0}", file.name);
					continue;;
				}
			
				if (keyDict.ContainsKey(file.name))
				{
					Debug.LogErrorFormat("File is exist: {0}", file.name);
					continue;;
				}
				
				var keys = new List<string>();
				keyDict.Add(file.name, keys);
				
				foreach (var line in content)
				{
					try
					{
						var key = line[0];
						if (keys.Contains(key) == false)
						{
							keys.Add(key);
						}
						else
						{
							Debug.LogWarningFormat("Key is exist: {0}", key);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
						throw;
					}
				}
			}
			return keyDict;
		}

		public void WriteFile(string code, string path)
		{
			var writeFolder = path.Substring(0, path.IndexOf(SearchKey, StringComparison.Ordinal)) + ScriptPath;
			if (!Directory.Exists (writeFolder))
				Directory.CreateDirectory (writeFolder);
			
			File.WriteAllText (writeFolder + DefaultClassName + ".cs", code);
		}

		public void GenFullProcess(string path)
		{
			var keyDict = CollectKeys(path);
			
			var code = Generate(keyDict);
			
			WriteFile(code, path);
		}
	}
    
#if UNITY_EDITOR
    public class CSVImportExamplePostprocessor : AssetPostprocessor
    {
	    private static readonly CodeGenerator Generator = new CodeGenerator();
	    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	    {
		    foreach (string str in importedAssets)
		    {
			    if (str.IndexOf(CodeGenerator.SearchKey, StringComparison.Ordinal) != -1)
			    {
					Generator.GenFullProcess(str);
			    }
		    }
		    AssetDatabase.Refresh();
	    }
    }
#endif
}
