using System;
using System.Collections.Generic;
using UnityEngine;
using Zitga.CsvTools;
#if UNITY_EDITOR
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine.Networking;

#endif

[CreateAssetMenu(fileName = "Language", menuName = "Localization/Language Data", order = 1)]
public class LanguageData : ScriptableObject
{
    public string downloadPath;
    public string saveFilePath;
    public List<string> codeList;
    public List<LanguageItem> itemList;
}

[Serializable]
public class LanguageItem
{
    public string sheetName;
    public string sheetId;
    public bool download = true;
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(LanguageItem))]
public class LanguageItemDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        var amountRect = new Rect(position.x, position.y, 60,
            position.height);
        var unitRect = new Rect(position.x + 65, position.y, 80,
            position.height);
        var downLoadRect = new Rect(position.x + 150, position.y, 155,
            position.height);

        // Draw fields - passs GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("sheetName"), GUIContent.none);
        EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("sheetId"), GUIContent.none);
        EditorGUI.PropertyField(downLoadRect, property.FindPropertyRelative("download"), GUIContent.none);


        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}

[CustomEditor(typeof(LanguageData))]
public class LevelScriptEditor : Editor
{
    LanguageData data;
    static string contentDownloading = string.Empty;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        data = (LanguageData) target;
        if (contentDownloading.Equals(string.Empty))
        {
            if (GUILayout.Button("Select All"))
            {
                SelectAllLanguage();
            }

            if (GUILayout.Button("Download Data Language"))
            {
                DownLoadFileLanguage();
            }
        }
        else
        {
            EditorGUILayout.HelpBox($"Download: {contentDownloading}", MessageType.None);
        }
    }

    [ContextMenu("DowLoadFileLanguage")]
    public void DownLoadFileLanguage()
    {
        if (EditorUtility.DisplayDialog("Download language",
            "Do you want to download language", "OK", "Cancel"))
        {
            UniTask.Run(() =>
            {
                DownloadLanguage().Forget();
            });
        }
    }

    public void SelectAllLanguage()
    {
        bool selectAll = false;
        for (int i = 0; i < data.itemList.Count; i++)
        {
            if (data.itemList[i].download == false)
            {
                selectAll = true;
                break;
            }
        }

        for (int i = 0; i < data.itemList.Count; i++)
        {
            data.itemList[i].download = selectAll;
        }
    }

    async UniTaskVoid DownloadLanguage()
    {
        if (Directory.Exists(data.saveFilePath))
            Directory.Delete(data.saveFilePath, true);
        Directory.CreateDirectory(data.saveFilePath);

        await UniTask.SwitchToMainThread();

        for (int i = 0; i < data.itemList.Count; i++)
        {
            var languageItem = data.itemList[i];
            if (languageItem.download)
            {
                var path = $"{data.downloadPath}/export?format=csv&gid={languageItem.sheetId}";
                var request = await UnityWebRequest.Get(path).SendWebRequest().ToUniTask(Progress.CreateOnlyValueChanged<float>((percent) =>
                {
                    contentDownloading = $"{languageItem.sheetName} => {percent* 100}%";
                }));
                TextProcessing(request.downloadHandler.text, i);
            }
        }

        contentDownloading = string.Empty;

        EditorUtility.DisplayDialog("Download language",
            "Download language finish", "OK");
        AssetDatabase.Refresh();
    }

    void TextProcessing(string content, int index)
    {
        List<Language> langList = new List<Language>();
        var csvTable = CsvReader.ParseCsv(content);//CSVSerializer.ParseCSV(content, ',', true);
        var lang = csvTable[0];
        for (int i = 0; i < lang.Length; i++)
        {
            var languageCode = lang[i].Trim().Split('-');
            if (languageCode.Length == 2 &&
                langList.Find(x => x.code.Equals(languageCode[1])) == null &&
                data.codeList.Contains(languageCode[1]))
            {
                langList.Add(new Language(i, languageCode[1]));
                var folderPath = data.saveFilePath + "\\" + languageCode[1];
                if (Directory.Exists(folderPath) == false)
                {
                    Directory.CreateDirectory(folderPath);
                }
            }
        }

        for (int i = 1; i < csvTable.Count; i++)
        {
            lang = csvTable[i];
            var key = lang[0];
            foreach (Language language in langList)
            {
                if (language.keyDict.ContainsKey(key))
                {
                    Debug.Log("KeyExist: " + key);
                    continue;
                }

                if (key.Equals(string.Empty))
                {
                    Debug.Log("Key is empty: " + i);
                    continue;
                }

                try
                {
                    string localize = lang[language.index].Replace("\n", "\\n").Replace("\r", "").Replace("\"", "").Replace("\"\"", "\"");
                    language.keyDict.Add(key, localize);
                }
                catch (Exception e)
                {
                    Debug.Log($"Something Error: {language.code} - {language.index} \n=> {e}");
                    throw;
                }
            }
        }

        foreach (var language in langList)
        {
            if (!data.codeList.Contains(language.code))
                continue;

            WriteFileLocalize(data.itemList[index].sheetName.ToLowerInvariant(), language.code, language.keyDict);
        }
    }

    public void WriteFileLocalize(string popup, string languageCode, Dictionary<string, string> dict)
    {
        string contentWrite = "key~value\n";
        foreach (var item in dict)
        {
            if (!string.IsNullOrEmpty(item.Key))
            {
                contentWrite += $"{item.Key}~{item.Value}\n";
            }
        }

        var url = $"{data.saveFilePath}\\{languageCode}\\{popup}.csv";
        SafeWriteAllText(url, contentWrite);
    }

    public static bool SafeWriteAllText(string outFile, string text)
    {
        try
        {
            if (string.IsNullOrEmpty(outFile))
            {
                return false;
            }

            CheckFileAndCreateDirWhenNeeded(outFile);
            if (File.Exists(outFile))
            {
                File.SetAttributes(outFile, FileAttributes.Normal);
            }

            File.WriteAllText(outFile, text);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"SafeWriteAllText failed! path = {outFile} with err = {ex.Message}");
            return false;
        }
    }

    public static void CheckFileAndCreateDirWhenNeeded(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        FileInfo fileInfo = new FileInfo(filePath);
        DirectoryInfo dirInfo = fileInfo.Directory;
        if (dirInfo != null && !dirInfo.Exists)
        {
            Directory.CreateDirectory(dirInfo.FullName);
        }
    }
}

class Language
{
    public Language(int index, string code)
    {
        this.code = code;
        this.index = index;
        keyDict = new Dictionary<string, string>();
    }

    public readonly int index;
    public readonly string code;
    public readonly Dictionary<string, string> keyDict;
}
#endif