using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZitgaPackageManager;

namespace ZitgaPackageManager.Models
{
    public class ProviderModel
    {
        public ZBaseEnum.Status currentStatues;
        public string providerName;
        public string displayProviderName;
        public string currentUnityVersion;
        public string latestUnityVersion;
        public string downloadURL;
        public string hash;
        public ZBaseEnum.Source source;
        public Dictionary<string, string> dependencies;

        public ProviderModel ShallowCopy()
        {
            return (ProviderModel)this.MemberwiseClone();
        }


        public ProviderModel()
        {
            currentStatues = ZBaseEnum.Status.none;
            providerName = displayProviderName = string.Empty;
            source = ZBaseEnum.Source.registry;
            downloadURL = string.Empty;
            currentUnityVersion = "none";
            dependencies = new Dictionary<string, string>();
        }

        public ProviderModel(string providerName, string displayName, string currVer, string lastVer, ZBaseEnum.Status currStatus, ZBaseEnum.Source source, string urlDownload = "")
        {
            this.providerName = providerName;
            this.displayProviderName = displayName;
            this.currentStatues = currStatus;
            this.currentUnityVersion = currVer;
            this.latestUnityVersion = lastVer;
            this.source = source;
            this.downloadURL = urlDownload;
        }



        public bool GetFromJson(string name, Dictionary<string, object> dic)
        {
            providerName = name;
            object obj;

            //source
            dic.TryGetValue("source", out obj);
            if (obj != null)
            {
                ZBaseEnum.Source result;
                if (Enum.TryParse(obj as string, out result))
                {
                    this.source = result;
                }
            }
            //display name
            if (ZBasePackageIdConfig.ListPackages.ContainsKey(name))
                this.displayProviderName = ZBasePackageIdConfig.ListPackages[name];
            //version, url
            dic.TryGetValue("version", out obj);
            if (obj != null)
            {
                if (this.source == ZBaseEnum.Source.registry)
                {
                    this.currentUnityVersion = this.latestUnityVersion = obj as string;
                }
                else if (this.source == ZBaseEnum.Source.git)
                {
                    string objString = obj as string;
                    string[] arrString = objString.Split('#'); // url = urlGit + # + version
                    if (arrString.Length >= 2)
                    {
                        this.downloadURL = arrString[0];
                        this.currentUnityVersion = this.latestUnityVersion = arrString[1];
                    }
                }
            }
            //hash
            dic.TryGetValue("hash", out obj);
            if (obj != null)
            {

                this.hash = obj as string;
                this.hash = this.hash.Remove(10);
            }
            //dependencies
            dic.TryGetValue("dependencies", out obj);
            if (obj != null)
            {
                Dictionary<string, object> dependenciesData = obj as Dictionary<string, object>;
                foreach (var item in dependenciesData)
                {
                    this.dependencies.Add(item.Key, item.Value as string);
                }
            }

            return true;
        }

        public void GetVersionInfoFromServer(Dictionary<string, object> data)
        {
            foreach (var item in data)
            {
                try
                {
                    if (item.Key.ToLower().Equals("version"))
                    {
                        this.currentUnityVersion = this.latestUnityVersion = item.Value as string;
                    }

                }
                catch (Exception e)
                {
                    Debug.Log("Error parse tool version info " + e.ToString());
                }
            }

            Debug.Log(string.Format("***Pack {0} on server, version {1}***", this.displayProviderName, this.latestUnityVersion));
        }

        public void GetVersionInfoFromLocal(Dictionary<string, object> data)
        {
            foreach (var item in data)
            {
                try
                {
                    if (item.Key.ToLower().Equals("version"))
                    {
                        this.currentUnityVersion = item.Value as string;
                    }

                }
                catch (Exception e)
                {
                    Debug.Log("Error parse tool version info " + e.ToString());
                }
            }

            Debug.Log(string.Format("***Pack {0} on local, version {1}***", this.displayProviderName, this.currentUnityVersion));
        }
    }
}
