using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Zitga.CsvTools.Tutorials
{
    public class SpawnEnemyExample : ScriptableObject
    {
        [Serializable]
        public class Bonus
        {
            public int typeId, number, interval, bonusHp, bonusMoveSpeed, bonusAtk;
        }
    
        [Serializable]
        public class SpawnEnemy
        {
            public int timeStart;
            public int timeEnd;
            public int[] zoneId;
            public Bonus[] bonuses;
        }
    
        public SpawnEnemy[] spawnEnemies;
    }
    
    #if UNITY_EDITOR
    public class SpawnEnemyPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                if (str.IndexOf("/spawn_enemy.csv", StringComparison.Ordinal) != -1)
                {
                    TextAsset data = AssetDatabase.LoadAssetAtPath<TextAsset>(str);
                    string assetFile = str.Replace(".csv", ".asset");
                    SpawnEnemyExample gm = AssetDatabase.LoadAssetAtPath<SpawnEnemyExample>(assetFile);
                    if (gm == null)
                    {
                        gm = ScriptableObject.CreateInstance<SpawnEnemyExample>();
                        AssetDatabase.CreateAsset(gm, assetFile);
                    }
    
                    gm.spawnEnemies = CsvReader.Deserialize<SpawnEnemyExample.SpawnEnemy>(data.text);
    
                    EditorUtility.SetDirty(gm);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Reimport Asset: " + str);
                }
            }
        }
    }
    #endif
}
