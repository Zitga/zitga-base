using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Zitga.CsvTools.Tutorials
{
    public class ConstData : ScriptableObject
    {
        public int maxhp;
        public string type;
        public int[] intarray;
        public float[] floatarray;
        public string[] stringarray;
    }

#if UNITY_EDITOR
    public class ConstPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                if (str.IndexOf("/const.csv", StringComparison.Ordinal) != -1)
                {
                    TextAsset data = AssetDatabase.LoadAssetAtPath<TextAsset>(str);
                    string assetFile = str.Replace(".csv", ".asset");
                    ConstData gm = AssetDatabase.LoadAssetAtPath<ConstData>(assetFile);
                    if (gm == null)
                    {
                        gm = ScriptableObject.CreateInstance<ConstData>();
                        AssetDatabase.CreateAsset(gm, assetFile);
                    }

                    ConstData readData = CsvReader.DeserializeIdValue<ConstData>(data.text);
                    EditorUtility.CopySerialized(readData, gm);

                    EditorUtility.SetDirty(gm);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Reimport Asset: " + str);
                }
            }
        }
    }
#endif
}