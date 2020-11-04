using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using ZBaseJsonHelper;
using ZitgaPackageManager.Models;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ZitgaPackageManager.Editors
{
    public class ZBaseDependenciesManager : EditorWindow
    {
        private const int Width = 1000;
        private const int Height = 600;
        private const int LoadDataComplete = 3;
        private const string InstallURL = "https://github.com/Zitga/{0}.git?path=Packages/{1}";
        private const string PackLockURL = "https://github.com/Zitga/{0}/raw/master/Packages/packages-lock.json";
        private const string PackVersionURL = "https://github.com/Zitga/{0}/raw/master/Packages/{1}/package.json";
        private const string PackDownloadURL = "https://github.com/Zitga/{0}/raw/master/Assets/PackageManagerDownload/{1}";
        private const string PackIdConfigURL = "https://github.com/Zitga/{0}/raw/master/Assets/AssetConfig/package_id_config.json";
        //
        private const string SuffixesVersionGitURL = "#{0}";
        private const string PackLockLocalDir = "Packages/packages-lock.json";
        private const string PackVersionLocalDir = "Packages/{0}/package.json";
        private const string PackCacheLocalDir = "Library/PackageCache/{0}@{1}/package.json";
        private const string PackManagerDownloadDir = "Library/PackageCache/{0}@{1}/{2}";
        private const string InstallPackLocalDir = "Library/PackageCache/{0}@{1}/FilePackage/{2}.unitypackage";
        private const string ManifestURL = "Packages/manifest.json";

        private GUIStyle headerStyle;
        private GUIStyle textStyle;
        private GUIStyle textVersionStyle;
        private readonly GUILayoutOption buttonWidth = GUILayout.Width(70);
        private readonly GUILayoutOption buttonHeight = GUILayout.Height(25);
        private Vector2 scrollPos;

        private Dictionary<string, ProviderModel> providersSet = new Dictionary<string, ProviderModel>();
        private Dictionary<string, ProviderModel> providersLocal = new Dictionary<string, ProviderModel>();
        private ZBaseEditorCoroutines mEditorCoroutines;
        private int progressLoadData = 0;
        private bool isProcessing;
        private bool canRefresh;

        //Multi request
        private AddRequest remRequest;
        private List<string> pkgNameQueue = new List<string>();
        private Queue<string> urlQueue = new Queue<string>();
        private bool isAddMultiPkg = false;

        public static void ShowZBaseDependenciesManager()
        {
            var win = GetWindowWithRect<ZBaseDependenciesManager>(new Rect(0, 0, Width, Height), true);
            win.titleContent = new GUIContent("Zitga Package Manager");
            win.Focus();
        }

        void Awake()
        {
            headerStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                fixedHeight = 20,
                stretchWidth = true,
                fixedWidth = Width / 4 + 5,
                clipping = TextClipping.Overflow,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 100, 0, 0),
            };

            textStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Normal,
                clipping = TextClipping.Overflow,
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 25,
            };

            textVersionStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Normal,
                clipping = TextClipping.Overflow,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(50, 100, 0, 0),
                fixedHeight = 25,
            };

            EditorPrefs.SetString("key_package_import", string.Empty);
            CheckVersion();

        }

        void OnDestroy()
        {
            CancelDownload();
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawToolHeader();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                DrawProviderManager();
                GUILayout.Space(5);
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }

            GUILayout.Space(15);
            DrawPackageHeader();
            GUILayout.Space(15);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(1000), GUILayout.Height(400));
            foreach (var provider in providersLocal)
            {
                if (provider.Value.providerName == ZBasePackageIdConfig.NamePackageManager)
                    continue;

                DrawProviderItem(provider.Value);
                GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
            {
                GUILayout.Space(938);
                if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(30)) && !isProcessing)
                {
                    Refresh();
                }
            }
        }

        void OnInspectorUpdate()
        {
            if (canRefresh)
            {
                Debug.Log("**********Refresh*************");
                Refresh();
            }
        }

        #region Funnction
        private void CheckVersion()
        {
            progressLoadData = 0;
            GetPackageIdConfig();
            mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetVersionFromPackageLockLocal());
        }

        private void CancelDownload()
        {
            isProcessing = false;

            if (mEditorCoroutines != null)
            {
                mEditorCoroutines.StopEditorCoroutine();
                mEditorCoroutines = null;
            }
        }

        private void Refresh()
        {
            canRefresh = false;
            CancelDownload();
            CheckVersion();
        }
        #endregion


        #region UI General
        private void DrawToolHeader()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
            {
                EditorGUILayout.LabelField("Current Tool Version", new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 13,
                    fixedHeight = 20,
                    stretchWidth = true,
                    fixedWidth = Width / 4,
                    clipping = TextClipping.Overflow,
                    padding = new RectOffset(Width / 4 + 15, 0, 0, 0),
                });

                GUILayout.Space(85);
                EditorGUILayout.LabelField("Latest Tool Version", new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 13,
                    fixedHeight = 20,
                    stretchWidth = true,
                    fixedWidth = Screen.width / 4,
                    clipping = TextClipping.Overflow,
                });
            }
        }

        private void DrawPackageHeader()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
            {
                EditorGUILayout.LabelField("Package", headerStyle);
                EditorGUILayout.LabelField("Current Package Version", headerStyle);
                EditorGUILayout.LabelField("Latest Package Version", headerStyle);
                GUILayout.Space(25);
                EditorGUILayout.LabelField("Action", headerStyle);
            }
        }

        void DrawProviderManager()
        {
            if (providersLocal.ContainsKey(ZBasePackageIdConfig.NamePackageManager))
            {
                ProviderModel providerData = providersLocal[ZBasePackageIdConfig.NamePackageManager];
                DrawProviderItem(providerData);
            }
        }

        void DrawProviderItem(ProviderModel providerData)
        {
            if (providerData == null)
                return;

            if (!providerData.Equals(default(ProviderModel)))
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                {
                    GUI.enabled = true;

                    EditorGUILayout.LabelField(providerData.displayProviderName, textStyle);
                    EditorGUILayout.LabelField(providerData.currentUnityVersion, textVersionStyle);
                    EditorGUILayout.LabelField(providerData.latestUnityVersion, textVersionStyle);

                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
                    {
                        if (providerData.currentStatues == ZBaseEnum.Status.none)
                        {
                            GUILayout.Space(35);
                            if (providerData.providerName.StartsWith("com"))
                            {
                                InstallButton(providerData);
                            }
                            else
                            {
                                DownloadButton(providerData);
                            }
                            GUILayout.Space(35);
                        }
                        else if (providerData.currentStatues == ZBaseEnum.Status.installed)
                        {
                            if (providerData.providerName.StartsWith("com"))
                            {
                                UpdatedButton(providerData);
                            }
                            else
                            {
                                ImportButton(providerData);
                            }
                        }
                        else
                        {
                            UpdateButtonDisable();
                        }

                        if (providerData.currentStatues != ZBaseEnum.Status.none && providerData.providerName != ZBasePackageIdConfig.NamePackageManager)
                        {
                            RemoveButton(providerData);
                        }
                    }

                    GUILayout.Space(5);
                    GUI.enabled = true;
                }
            }
        }
        #endregion

        #region BUTTON
        private void InstallButton(ProviderModel providerData)
        {
            bool btn = GUILayout.Button(new GUIContent
            {
                text = "Install",
            }, buttonWidth, buttonHeight);
            if (btn && !isProcessing)
            {
                GUI.enabled = true;
                try
                {
                    Debug.LogWarning(">>>>>>>>> Install Click! <<<<<<<<<<");
                    if (providersSet[providerData.providerName].dependencies.Count == 0)
                    {
                        ZBaseEditorCoroutines.StartEditorCoroutine(AddPackage(providerData, (result) =>
                        {
                            if (result.Status == StatusCode.Success)
                            {
                                Debug.Log(string.Format("***Install Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                canRefresh = true;
                            }
                        }));
                    }
                    else
                    {
                        ZBaseEditorCoroutines.StartEditorCoroutine(AddPackageWithDependencie(providerData, (result) =>
                        {
                            if (result.Status == StatusCode.Success)
                            {
                                Debug.Log(string.Format("***Install Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                EditorApplication.UnlockReloadAssemblies();
                                canRefresh = true;
                            }
                        }));
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error " + e.Message);
                }
            }
        }

        private void DownloadButton(ProviderModel providerData)
        {
            bool btn = GUILayout.Button(new GUIContent
            {
                text = "Download",
            }, buttonWidth, buttonHeight);
            if (btn && !isProcessing)
            {
                GUI.enabled = true;
                try
                {
                    Debug.LogWarning(">>>>>>>>> Download Click! <<<<<<<<<<");
                    if (providersSet[providerData.providerName].dependencies.Count == 0)
                    {
                        ZBaseEditorCoroutines.StartEditorCoroutine(AddPackage(providerData, (result) =>
                        {
                            if (result.Status == StatusCode.Success)
                            {
                                Debug.Log(string.Format("***Download Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                canRefresh = true;
                                EditorPrefs.SetString("key_package_import", providerData.providerName);
                            }
                        }));
                    }
                    else
                    {
                        ZBaseEditorCoroutines.StartEditorCoroutine(AddPackageWithDependencie(providerData, (result) =>
                        {
                            if (result.Status == StatusCode.Success)
                            {
                                Debug.Log(string.Format("***Download Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                EditorApplication.UnlockReloadAssemblies();
                                canRefresh = true;
                                EditorPrefs.SetString("key_package_import", providerData.providerName);
                            }
                        }));
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error " + e.Message);
                }
            }
        }

        private void UpdatedButton(ProviderModel providerData, bool isDisable = false)
        {
            if (isDisable)
            {
                GUI.enabled = false;
                GUILayout.Button(new GUIContent
                {
                    text = "Updated",
                }, buttonWidth, buttonHeight);
            }
            else
            {
                var btn = GUILayout.Button(new GUIContent
                {
                    text = "Update",
                }, buttonWidth, buttonHeight);
                if (btn && !isProcessing)
                {
                    GUI.enabled = true;
                    try
                    {
                        Debug.LogWarning(">>>>>>>>> Update Click! <<<<<<<<<<");
                        if (providersSet[providerData.providerName].dependencies.Count == 0)
                        {
                            ZBaseEditorCoroutines.StartEditorCoroutine(AddPackage(providerData, (result) =>
                            {
                                if (result.Status == StatusCode.Success)
                                {
                                    Debug.Log(string.Format("***Update Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                    canRefresh = true;
                                }
                            }));
                        }
                        else
                        {
                            ZBaseEditorCoroutines.StartEditorCoroutine(AddPackageWithDependencie(providerData, (result) =>
                            {
                                if (result.Status == StatusCode.Success)
                                {
                                    Debug.Log(string.Format("***Update Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                    EditorApplication.UnlockReloadAssemblies();
                                    canRefresh = true;
                                }
                            }));
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error " + e.Message);
                    }
                }
            }
        }
        private void UpdateButtonDisable()
        {
            UpdatedButton(null, true);
        }

        private void ImportButton(ProviderModel providerData)
        {
            bool btn = GUILayout.Button(new GUIContent
            {
                text = "Import",
            }, buttonWidth, buttonHeight);
            if (btn && !isProcessing)
            {
                GUI.enabled = true;
                try
                {
                    Debug.LogWarning(">>>>>>>>> Import Click! <<<<<<<<<<");
                    ImportPackage(providerData);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error " + e.Message);
                }
            }
        }

        private void RemoveButton(ProviderModel providerData)
        {
            GUI.enabled = true;
            var btn = GUILayout.Button(new GUIContent
            {
                text = "Remove",
            }, buttonWidth, buttonHeight);
            if (btn && !isProcessing)
            {
                GUI.enabled = true;
                try
                {
                    Debug.LogWarning(">>>>>>>>> Remove Click! <<<<<<<<<<");

                    if (EditorUtility.DisplayDialog("Remove Package", "Are you sure you want to remove this package?", "Remove", "Cancle"))
                    {
                        ZBaseEditorCoroutines.StartEditorCoroutine(RemovePackage(providerData.providerName, (result) =>
                        {
                            if (result.Status == StatusCode.Success)
                            {
                                Debug.Log(string.Format("***Remove Success {0} {1}***", providerData.providerName, providerData.latestUnityVersion));
                                canRefresh = true;
                            }
                        }));
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error " + e.Message);
                }
            }
        }
        #endregion

        #region Action
        private IEnumerator AddPackageWithDependencie(ProviderModel providerInfo, System.Action<AddRequest> callback)
        {
            pkgNameQueue.Clear();
            urlQueue.Clear();

            foreach (var item in providersSet[providerInfo.providerName].dependencies)
            {
                if (providersLocal.Keys.Contains(item.Key) && providersLocal[item.Key].currentStatues != ZBaseEnum.Status.none)
                {
                    continue;
                }
                pkgNameQueue.Add(item.Key);
            }

            AddMultiPackage();

            while (isAddMultiPkg)
            {
                isProcessing = true;
                yield return new WaitForSeconds(0.1f);
            }

            ZBaseEditorCoroutines.StartEditorCoroutine(AddPackage(providerInfo, callback));
        }


        private void AddMultiPackage()
        {
            if (pkgNameQueue.Count == 0)
            {
                return;
            }

            isAddMultiPkg = true;

            ProviderModel providerSever = null;
            bool isRegistry = false;

            foreach (var item in pkgNameQueue)
            {
                string urlDownload = "";
                isRegistry = false;

                if (providersSet.Keys.Contains(item))
                {
                    providerSever = providersSet[item];

                    ZBaseEditorCoroutines.StartEditorCoroutine(SearchPackage(item, (resultSearch) =>
                    {
                        if (resultSearch != null)
                        {
                            if (resultSearch.Result.Length > 0)
                                isRegistry = true;
                        }
                    }));

                    if (isRegistry)
                    {
                        urlDownload = item;

                    }
                    else
                    {
                        if (providerSever.source == ZBaseEnum.Source.git)
                            urlDownload = providerSever.downloadURL + string.Format(SuffixesVersionGitURL, providerSever.latestUnityVersion);
                        else if (providerSever.source == ZBaseEnum.Source.embedded)
                            urlDownload = string.Format(InstallURL, ZBasePackageIdConfig.Repo, providerSever.providerName);
                        else if (providerSever.source == ZBaseEnum.Source.registry)
                            urlDownload = providerSever.providerName;
                    }
                }
                else
                {
                    urlDownload = item;
                }


                urlQueue.Enqueue(urlDownload);
            }

            EditorApplication.update += PackageInstallProgress;
            EditorApplication.LockReloadAssemblies();

            remRequest = Client.Add(urlQueue.Dequeue());
        }

        void PackageInstallProgress()
        {
            if (remRequest.IsCompleted)
            {
                switch (remRequest.Status)
                {
                    case StatusCode.Failure:
                        Debug.LogError("Couldn't install package '" + remRequest.Result.displayName + "': " + remRequest.Error.message);
                        break;

                    case StatusCode.InProgress:
                        break;

                    case StatusCode.Success:
                        Debug.Log("Installed package: " + remRequest.Result.displayName);
                        break;
                }

                if (urlQueue.Count > 0)
                {
                    remRequest = Client.Add(urlQueue.Dequeue());
                }
                else
                {    // no more packages to remove
                    EditorApplication.update -= PackageInstallProgress;
                    isAddMultiPkg = false;
                }

            }
        }

        private IEnumerator AddPackage(ProviderModel providerInfo, System.Action<AddRequest> callback)
        {
            AddRequest result = null;
            string urlDownload = "";
            ProviderModel providerSever = providersSet[providerInfo.providerName];

            if (providerSever.source == ZBaseEnum.Source.git)
                urlDownload = providerInfo.downloadURL + string.Format(SuffixesVersionGitURL, providerInfo.latestUnityVersion);
            else if (providerSever.source == ZBaseEnum.Source.embedded)
                urlDownload = string.Format(InstallURL, ZBasePackageIdConfig.Repo, providerInfo.providerName);
            else if (providerSever.source == ZBaseEnum.Source.registry)
                urlDownload = providerInfo.providerName;

            result = Client.Add(urlDownload);

            while (!result.IsCompleted)
            {
                isProcessing = true;
                yield return new WaitForSeconds(0.1f);
            }


            if (result.Error != null)
            {
                Debug.LogError("[Error] Add Fail: " + result.Error.message);
                if (callback != null)
                    callback(null);
            }
            else
            {
                if (callback != null)
                    callback(result);
            }
        }

        private IEnumerator SearchPackage(string PackageName, System.Action<SearchRequest> callback)
        {
            var result = Client.Search(PackageName);

            while (!result.IsCompleted)
            {
                isProcessing = true;
                yield return new WaitForSeconds(0.1f);
            }

            if (result.Error != null)
            {
                if (callback != null)
                    callback(null);
            }
            else
            {
                if (callback != null)
                    callback(result);
            }
        }

        private IEnumerator RemovePackage(string PackageName, System.Action<RemoveRequest> callback)
        {
            var result = Client.Remove(PackageName);

            while (!result.IsCompleted)
            {
                isProcessing = true;
                yield return new WaitForSeconds(0.1f);
            }

            if (result.Error != null)
            {
                Debug.LogError("[Error] Add Fail: " + result.Error.message);
                if (callback != null)
                    callback(null);
            }
            else
            {
                if (callback != null)
                    callback(result);
            }
        }

        private void ImportPackage(ProviderModel providerModel)
        {
            string urlPackageImport = string.Format(InstallPackLocalDir, providerModel.providerName, providersLocal[providerModel.providerName].hash, providerModel.displayProviderName);
            if (CheckFileExist(urlPackageImport))
                AssetDatabase.ImportPackage(urlPackageImport, true);
            else
                Debug.LogError("File import not found!");
        }
        #endregion

        #region Http       
        private void GetPackageIdConfig()
        {
            string urlPackageIdConfig = string.Format(PackIdConfigURL, ZBasePackageIdConfig.Repo);
            mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetRequest(urlPackageIdConfig, (result) => GetDataFromPackageConfig(result)));
        }
        private void GetPackageLockServer()
        {
            string urlPackageLock = string.Format(PackLockURL, ZBasePackageIdConfig.Repo);
            mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetRequest(urlPackageLock, (result) => GetDataFromPackageLockServer(result)));
        }

        private IEnumerator GetVersionForEmbeddedPack()
        {
            int numbQuest = 0;
            foreach (var item in providersSet)
            {
                ProviderModel info = item.Value;
                if (info.source == ZBaseEnum.Source.embedded)
                {
                    numbQuest++;
                    GetPackageFromServer(info.providerName, delegate (Dictionary<string, object> result)
                    {
                        info.GetVersionInfoFromServer(result);
                        numbQuest--;
                    });
                }
            }

            while (numbQuest > 0)
            {
                yield return new WaitForSeconds(0.1f);
            }

            progressLoadData++;
        }

        private void GetPackageFromServer(string packageName, System.Action<Dictionary<string, object>> callback)
        {
            string urlPackage = string.Format(PackVersionURL, ZBasePackageIdConfig.Repo, packageName);
            mEditorCoroutines = ZBaseEditorCoroutines.StartEditorCoroutine(GetRequest(urlPackage, (result) => callback(result)));
        }

        private IEnumerator GetRequest(string url, System.Action<Dictionary<string, object>> callback)
        {
            UnityWebRequest unityWebRequest = UnityWebRequest.Get(url);
            var webRequest = unityWebRequest.SendWebRequest();

            if (!unityWebRequest.isHttpError && !unityWebRequest.isNetworkError)
            {
                Debug.Log("[Get] URL: " + url);
                while (!webRequest.isDone)
                {
                    yield return new WaitForSeconds(0.1f);
                    if (EditorUtility.DisplayCancelableProgressBar("Downloading...", "", webRequest.progress))
                    {
                        Debug.LogError(string.Format("[Get] URL: {0}\n{1}", url, unityWebRequest.error));
                        CancelDownload();
                    }
                }
                EditorUtility.ClearProgressBar();

                string json = unityWebRequest.downloadHandler.text;
                Debug.Log("Data: " + json);

                Dictionary<string, object> dic = new Dictionary<string, object>();
                //            
                try
                {
                    dic = Json.Deserialize(json) as Dictionary<string, object>;
                    if (callback != null)
                        callback(dic);
                }

                catch (Exception e)
                {
                    Debug.LogError("[Get] URL: " + url + "\n" + "[Parse Data] Error: " + e.ToString());
                }

            }
            else
            {
                Debug.LogError("[Error] Load Fail: " + unityWebRequest.error);
            }
        }

        private IEnumerator DownloadFile(string downloadFileUrl, string downloadFileName, System.Action callback)
        {
            string fileDownloading = "";

            Dictionary<string, string> listRequest = new Dictionary<string, string>();
            listRequest.Add(downloadFileName, downloadFileUrl);
            listRequest.Add(downloadFileName + ".meta", downloadFileUrl + ".meta");
            foreach (var item in listRequest)
            {
                string path = string.Format(PackManagerDownloadDir, ZBasePackageIdConfig.NamePackageManager, providersLocal[ZBasePackageIdConfig.NamePackageManager].hash, item.Key);
                fileDownloading = string.Format("Downloading {0}", item.Key);

                UnityWebRequest downloadWebClient = new UnityWebRequest(item.Value);
                downloadWebClient.downloadHandler = new DownloadHandlerFile(path);
                downloadWebClient.SendWebRequest();

                if (!downloadWebClient.isHttpError && !downloadWebClient.isNetworkError)
                {
                    while (!downloadWebClient.isDone)
                    {
                        isProcessing = true;
                        yield return new WaitForSeconds(0.1f);
                        if (EditorUtility.DisplayCancelableProgressBar("Download Manager", fileDownloading, downloadWebClient.downloadProgress))
                        {
                            Debug.LogError(downloadWebClient.error);
                            CancelDownload();
                            downloadWebClient.Dispose();
                        }
                    }
                }
                else
                {
                    Debug.LogError("Error Downloading " + downloadFileName + " : " + downloadWebClient.error);
                    CancelDownload();
                    downloadWebClient.Dispose();
                }
            }

            EditorUtility.ClearProgressBar();

            yield return new WaitForSeconds(0.1f);
            if (callback != null)
            {
                callback.Invoke();
            }
        }
        #endregion

        #region Parse Data
        // server       
        private void GetDataFromPackageConfig(Dictionary<string, object> data)
        {
            ZBasePackageIdConfig.ListPackages.Clear();

            try
            {
                if (data.Count > 0)
                {
                    foreach (var item in data)
                    {
                        ZBasePackageIdConfig.ListPackages.Add(item.Key, item.Value.ToString());
                    }
                }

                progressLoadData++;
                GetPackageLockServer();
            }
            catch (Exception e)
            {
                Debug.LogError("Error Get Version From Package Lock Server: " + e.Message);
            }
        }

        private void GetDataFromPackageLockServer(Dictionary<string, object> data)
        {
            providersSet.Clear();

            try
            {
                object dependencies;

                if (data.TryGetValue("dependencies", out dependencies))
                {
                    if (dependencies != null)
                    {
                        foreach (var item in dependencies as Dictionary<string, object>)
                        {
                            ProviderModel info = new ProviderModel();
                            if (ZBasePackageIdConfig.ListPackages.ContainsKey(item.Key))
                            {
                                if (info.GetFromJson(item.Key, item.Value as Dictionary<string, object>))
                                {
                                    providersSet.Add(info.providerName, info);
                                    if (info.currentUnityVersion != "none")
                                        Debug.Log(string.Format("***Package {0} on server, version {1}***", info.displayProviderName, info.latestUnityVersion));
                                }
                            }
                        }
                    }
                }

                progressLoadData++;

                ZBaseEditorCoroutines.StartEditorCoroutine(GetVersionForEmbeddedPack());
            }
            catch (Exception e)
            {
                Debug.LogError("Error Get Version From Package Lock Server: " + e.Message);
            }
        }


        // local
        private IEnumerator GetVersionFromPackageLockLocal()
        {

            while (!IsLoadDataServerDone())
            {
                yield return new WaitForSeconds(0.1f);
            }

            Dictionary<string, object> dic = new Dictionary<string, object>();
            providersLocal.Clear();

            try
            {
                string fileContent = File.ReadAllText(PackLockLocalDir);
                dic = Json.Deserialize(fileContent) as Dictionary<string, object>;
                object dependencies;
                if (dic.TryGetValue("dependencies", out dependencies))
                {
                    if (dependencies != null)
                    {

                        foreach (var item in dependencies as Dictionary<string, object>)
                        {
                            ProviderModel info = new ProviderModel();
                            if (ZBasePackageIdConfig.ListPackages.ContainsKey(item.Key))
                            {
                                if (info.GetFromJson(item.Key, item.Value as Dictionary<string, object>))
                                {
                                    providersLocal.Add(info.providerName, info);
                                    if (info.currentUnityVersion != "none")
                                        Debug.Log(string.Format(">>>Package {0} on local, version {1}<<<", info.displayProviderName, info.currentUnityVersion));
                                }
                            }
                        }

                        foreach (var item in providersLocal)
                        {
                            ProviderModel info = item.Value;
                            if (info.source == ZBaseEnum.Source.embedded && info.currentUnityVersion == "none")
                            {
                                LoadPackageFromLocal(info.providerName, info.GetVersionInfoFromLocal);
                            }
                            else if (info.source == ZBaseEnum.Source.git && info.currentUnityVersion == "none" && !string.IsNullOrEmpty(info.hash))
                            {
                                LoadPackageCacheFromLocal(info.providerName, info.hash, info.GetVersionInfoFromLocal);
                            }
                        }

                        CompareVersion();

                        //check package not install
                        if (providersLocal.Count != ZBasePackageIdConfig.ListPackages.Count) //skip item package manager
                        {
                            foreach (var item in ZBasePackageIdConfig.ListPackages)
                            {
                                if (providersLocal.ContainsKey(item.Key))
                                    continue;

                                if (!providersSet.ContainsKey(item.Key))
                                    continue;


                                ProviderModel info = providersSet[item.Key].ShallowCopy();
                                info.currentStatues = ZBaseEnum.Status.none;
                                info.currentUnityVersion = "none";
                                if (!item.Key.StartsWith("com"))
                                    info.source = ZBaseEnum.Source.package;

                                providersLocal.Add(info.providerName, info);

                                Debug.Log(string.Format(">>>Package {0} not install<<<", info.displayProviderName));
                            }
                        }

                    }
                }

                SortListLocal();

                ScopedRegistryConfig();
            }
            catch (Exception e)
            {
                Debug.Log("Error Get Version From Package Lock Local: " + e.Message);
            }

            var packageImport = EditorPrefs.GetString("key_package_import", string.Empty);
            if (!string.IsNullOrEmpty(packageImport))
            {
                if (providersSet.Keys.Contains(packageImport))
                    ImportPackage(providersSet[packageImport]);
                EditorPrefs.SetString("key_package_import", string.Empty);
            }

            Repaint();

        }

        private void LoadPackageFromLocal(string namePackage, System.Action<Dictionary<string, object>> callback)
        {
            try
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                string path = string.Format(PackVersionLocalDir, namePackage);
                string fileContent = File.ReadAllText(path);
                dic = Json.Deserialize(fileContent) as Dictionary<string, object>;

                if (dic.Count > 0)
                {
                    if (callback != null)
                        callback(dic);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error Load Package From Local: " + e.Message);
            }
        }

        private void LoadPackageCacheFromLocal(string namePackage, string hash, System.Action<Dictionary<string, object>> callback)
        {
            try
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                string path = string.Format(PackCacheLocalDir, namePackage, hash);
                string fileContent = File.ReadAllText(path);
                dic = Json.Deserialize(fileContent) as Dictionary<string, object>;

                if (dic.Count > 0)
                {
                    if (callback != null)
                        callback(dic);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error Load Package Cache From Local: " + e.Message);
            }
        }

        private bool IsLoadDataServerDone()
        {
            if (progressLoadData >= LoadDataComplete)
                return true;
            else
                return false;
        }

        #endregion

        #region Utility
        private void CompareVersion()
        {
            foreach (var item in providersLocal)
            {
                if (!providersSet.ContainsKey(item.Key))
                {
                    if (item.Key.StartsWith("com"))
                        item.Value.currentStatues = ZBaseEnum.Status.updated;
                    else
                        item.Value.currentStatues = ZBaseEnum.Status.installed;

                    item.Value.latestUnityVersion = item.Value.currentUnityVersion;
                }
                else
                {
                    var providerServer = providersSet[item.Key];
                    if (isNewerVersion(item.Value.currentUnityVersion, providerServer.latestUnityVersion))
                    {
                        item.Value.currentStatues = ZBaseEnum.Status.installed;
                    }
                    else
                    {
                        if (item.Key.StartsWith("com"))
                            item.Value.currentStatues = ZBaseEnum.Status.updated;
                        else
                            item.Value.currentStatues = ZBaseEnum.Status.installed;
                    }

                    item.Value.latestUnityVersion = providerServer.latestUnityVersion;
                }
            }
        }

        private bool isNewerVersion(string current, string latest)
        {
            bool isNewer = false;
            try
            {
                current = current.Replace("v", "");
                latest = latest.Replace("v", "");

                int[] currentVersion = Array.ConvertAll(current.Split('.'), int.Parse);
                int[] remoteVersion = Array.ConvertAll(latest.Split('.'), int.Parse);
                int remoteBuild = 0;
                int curBuild = 0;
                if (currentVersion.Length > 3)
                {
                    curBuild = currentVersion[3];
                }
                if (remoteVersion.Length > 3)
                {
                    remoteBuild = remoteVersion[3];

                }
                System.Version cur = new System.Version(currentVersion[0], currentVersion[1], currentVersion[2], curBuild);
                System.Version remote = new System.Version(remoteVersion[0], remoteVersion[1], remoteVersion[2], remoteBuild);
                isNewer = cur < remote;
            }
            catch (Exception e)
            {
                Debug.LogError("Error " + e.Message);
            }
            return isNewer;
        }
        private bool CheckFileExist(string pathFile)
        {
            return File.Exists(pathFile);
        }

        private void SortListLocal()
        {
            providersLocal = providersLocal.OrderBy(item => item.Value.displayProviderName).ToDictionary(item => item.Key, item => item.Value);
        }
        #endregion

        #region Scope
        private void ScopedRegistryConfig()
        {
            AddScopedRegistry(ZBasePackageIdConfig.ScopesGoogle);
        }

        internal void AddScopedRegistry(ScopedRegistry registry)
        {
            JObject manifestJSON = JObject.Parse(File.ReadAllText(ManifestURL));

            if (!CheckScopeExist(registry, manifestJSON))
            {
                AddOrCreateScopedRegistry(registry, manifestJSON);
                write(manifestJSON);
            }

        }

        private bool CheckScopeExist(ScopedRegistry registry, JObject manifestJSON)
        {
            JArray Jregistries = (JArray)manifestJSON["scopedRegistries"];
            if (Jregistries == null)
            {
                return false;
            }

            foreach (var JRegistryElement in Jregistries)
            {

                if (JRegistryElement["name"] != null && JRegistryElement["url"] != null)
                {

                    if (String.Equals(JRegistryElement["name"].ToString(), registry.name) && String.Equals(JRegistryElement["url"].ToString(), registry.url))
                    {
                        return true;
                    };
                }
            }

            return false;
        }

        private void AddOrCreateScopedRegistry(ScopedRegistry registry, JObject manifestJSON)
        {
            JArray Jregistries = (JArray)manifestJSON["scopedRegistries"];
            if (Jregistries == null)
            {
                Jregistries = new JArray();
                manifestJSON["scopedRegistries"] = Jregistries;
            }

            JObject JRegistry = new JObject();
            JRegistry["name"] = registry.name;
            JRegistry["url"] = registry.url;
            UpdateScope(registry, JRegistry);
            Jregistries.Add(JRegistry);
        }

        private void UpdateScope(ScopedRegistry registry, JToken registryElement)
        {
            JArray scopes = new JArray();
            foreach (var scope in registry.scopes)
            {
                scopes.Add(scope);
            }
            registryElement["scopes"] = scopes;
        }

        private void write(JObject manifestJSON)
        {
            File.WriteAllText(ManifestURL, manifestJSON.ToString());
            AssetDatabase.Refresh();
        }
        #endregion
    }
}