using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Zitga.CsvTools.Tutorials
{
    public class RankingData : ScriptableObject
    {
        public enum Country
        {
            gb = 1,
            de = 2,
            fi,
            be
        }

        [Serializable]
        public class Item
        {
            public int ranking;
            public string driver;
            public string constructor;
            public int score;
            public int podium;

            public Country country;
            public string[] win;
        }

        [Serializable]
        public class ItemDictionary : SerializableDictionary<int, Item>
        {
        }

        public ItemDictionary itemDict;
    }
    
#if UNITY_EDITOR
    public class RankingPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                if (str.IndexOf("/ranking.csv", StringComparison.Ordinal) != -1)
                {
                    TextAsset data = AssetDatabase.LoadAssetAtPath<TextAsset>(str);
                    string assetFile = str.Replace(".csv", ".asset");
                    RankingData gm = AssetDatabase.LoadAssetAtPath<RankingData>(assetFile);
                    if (gm == null)
                    {
                        gm = ScriptableObject.CreateInstance<RankingData>();
                        AssetDatabase.CreateAsset(gm, assetFile);
                    }

                    var items = CsvReader.Deserialize<RankingData.Item>(data.text);
                    gm.itemDict.Clear();
                    foreach (var item in items)
                    {
                        gm.itemDict.Add(item.ranking, item);
                    }

                    EditorUtility.SetDirty(gm);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Reimport Asset: " + str);
                }
            }
        }
    }
#endif   
}