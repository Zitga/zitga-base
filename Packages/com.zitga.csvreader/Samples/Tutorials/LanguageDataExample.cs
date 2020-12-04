using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Zitga.CsvTools.Tutorials
{
    public class LanguageDataExample : ScriptableObject
    {
        public StringStringDictionary data;
    }

#if UNITY_EDITOR
    public class LanguagePostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                if (str.IndexOf("/language.csv", StringComparison.Ordinal) != -1)
                {
                    TextAsset data = AssetDatabase.LoadAssetAtPath<TextAsset>(str);
                    string assetFile = str.Replace(".csv", ".asset");
                    LanguageDataExample gm = AssetDatabase.LoadAssetAtPath<LanguageDataExample>(assetFile);
                    if (gm == null)
                    {
                        gm = ScriptableObject.CreateInstance<LanguageDataExample>();
                        AssetDatabase.CreateAsset(gm, assetFile);
                    }
                
                    var rows = CsvReader.Deserialize<RowData>(data.text, '~');

                    gm.data.Clear();
                    foreach (var row in rows)
                    {
                        gm.data.Add(row.key, row.value);
                    }
                
                    EditorUtility.SetDirty(gm);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Reimport Asset: " + str);
                }
            }
        }
        
        public class RowData
        {
            public string key;
            public string value;
        }
    }
#endif
}