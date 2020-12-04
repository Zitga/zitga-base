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

using UnityEngine;
using UnityEditor;
using System;
using Localization.Samples.Localization.Editor;

namespace Loxodon.Framework.Editors
{
    public static class LocalizationMenu
	{
		private const string MENU_NAME = "Assets/Create/Localization/Gen C#";
		private const string EXTENSION = ".csv";

		[MenuItem (MENU_NAME, false, 0)]
		static void Generate ()
		{
			var selections = Selection.GetFiltered (typeof(TextAsset), SelectionMode.DeepAssets);
			if (selections == null || selections.Length <= 0)
				return;
			
			CodeGenerator generator = new CodeGenerator ();
			foreach (var s in selections) {
				try {
					string path = AssetDatabase.GetAssetPath (s);
					if (!path.ToLower ().EndsWith (EXTENSION))
						continue;

					generator.GenFullProcess(path);
					break;
				}
				catch (Exception)
				{
					// ignored
				}
			}
			
			AssetDatabase.Refresh();
		}



		[MenuItem (MENU_NAME, true)]
		static bool IsValidated ()
		{
			var selections = Selection.GetFiltered (typeof(TextAsset), SelectionMode.DeepAssets);
			if (selections == null || selections.Length <= 0)
				return false;
			
			foreach (var s in selections) {
				string path = AssetDatabase.GetAssetPath (s);
				if (path.ToLower ().EndsWith (EXTENSION))
					return true;
			}
			return false;
		}
	}
}

