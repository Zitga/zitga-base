using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Zitga.CsvTools.Tutorials
{
    public class ShopGroupExample : ScriptableObject
{
    [Serializable]
    public class Resource
    {
        public int resType;
        public int resId;
        public int resNumber;
    }
    [Serializable]
    public class Reward
    {
        public int moneyType;
        public int moneyValue;
    }

    [Serializable]
    public class RewardStock
    {
        public int id;
        public int rate;
        public int stock;
        public Resource[] resources;
        public Reward reward;
    }

    [Serializable]
    public class Shop
    {
        public int shopType;
        public int groupRate;
        public RewardStock[] rewardStocks;
        public bool enable;
    }
    
    [Serializable]
    public class ShopGroup
    {
        public int groupId;
        public int[] stageMin;
        public int stageMax;
        public Shop[] shops;
    }

    public ShopGroup[] shopGroups;
    
    [Serializable]
    public class ShopGroupDictionary : SerializableDictionary<int, ShopGroup> { }

    public ShopGroupDictionary shopDict;
}

#if UNITY_EDITOR
public class GroupPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string str in importedAssets)
        {
            if (str.IndexOf("/shop_group.csv", StringComparison.Ordinal) != -1)
            {
                TextAsset data = AssetDatabase.LoadAssetAtPath<TextAsset>(str);
                string assetFile = str.Replace(".csv", ".asset");
                ShopGroupExample gm = AssetDatabase.LoadAssetAtPath<ShopGroupExample>(assetFile);
                if (gm == null)
                {
                    gm = ScriptableObject.CreateInstance<ShopGroupExample>();
                    AssetDatabase.CreateAsset(gm, assetFile);
                }
                
                gm.shopGroups = CsvReader.Deserialize<ShopGroupExample.ShopGroup>(data.text);
                
                gm.shopDict.Clear();
                foreach (var shopGroup in gm.shopGroups)
                {
                    gm.shopDict.Add(shopGroup.groupId, shopGroup);
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
