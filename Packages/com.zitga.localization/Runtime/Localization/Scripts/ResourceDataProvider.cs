using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Zitga.LocalizeTools
{
    public class ResourceDataProvider : IDataProvider
    {
        private readonly Localization localization;
        
        private readonly Dictionary<string, LanguageData> data;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="localization"></param>
        public ResourceDataProvider(Localization localization)
        {
            this.localization = localization;
            
            data = new Dictionary<string, LanguageData>();
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void ClearData()
        {
            data.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async UniTask<LanguageData> Load(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                throw new Exception("category is empty");
            }
            
            if (IsContain(category))
            {
                throw new Exception("category is exist: " + category);
            }

            var path = $"Localization/Data/{localization.CultureInfo.Name}/{category}";
            try
            {
                var request = Resources.LoadAsync<LanguageData>(path);

                await UniTask.WaitUntil(()=> request.isDone);

                return (LanguageData)request.asset;
            }
            catch (Exception e)
            {
                throw new Exception("category is not exist: " + path);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async UniTask<string> Get(string category, string key)
        {
            if (IsContain(category) == false)
            {
                var categoryData = await Load(category);
                data.Add(category, categoryData);
            }
            
            try
            {
                return data[category].data[key];
            }
            catch (Exception e)
            {
                Debug.Log($"Key is not exist: {category}-{key} {e}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Check need load or not
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private bool IsContain(string category)
        {
            return data.ContainsKey(category);
        }
    }
}