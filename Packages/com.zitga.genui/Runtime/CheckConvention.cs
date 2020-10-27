using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GenUI
{
    public class CheckConvention
    {
        [MenuItem("Assets/GenUI/CheckNameConvention")]
        public static void CheckNameConvention()
        {
            string pattern = "^[a-z0-9_]+$";
            List<string> listNameError = new List<string>();

            foreach (var obj in Selection.objects)
            {
                if (!(obj is Sprite) && !(obj is Texture2D))
                {
                    Debug.LogError("Not support type " + obj.GetType().ToString());
                    continue;
                }

                string nameAsset = obj.name;
                if (!Regex.IsMatch(nameAsset, pattern) && !listNameError.Contains(nameAsset))
                    listNameError.Add(nameAsset);
            }

            StringBuilder message = new StringBuilder();
            StringBuilder nameReplace = new StringBuilder();

            foreach (string nameFile in listNameError)
            {
                message.Append(nameFile).Append("\n");
            }

            if (listNameError.Count > 0)
            {
                if (EditorUtility.DisplayDialog("List Files", message.ToString(), "Fix"))
                {
                    foreach (var obj in Selection.objects)
                    {
                        Debug.Log("Fix Click");
                        nameReplace.Clear();
                        string nameAsset = obj.name;
                        if (listNameError.Contains(nameAsset))
                        {
                            nameAsset = nameAsset.Replace(' ', '_');
                            string[] arrString = nameAsset.Split('_');
                            if (arrString != null)
                            {
                                for (int i = 0; i < arrString.Length; i++)
                                {
                                    nameReplace.Append(arrString[i].ToLower());
                                    if (i < arrString.Length - 1)
                                        nameReplace.Append("_");
                                }
                            }

                            string result = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), nameReplace.ToString());
                            if (!string.IsNullOrEmpty(result))
                                Debug.LogError(result);
                        }
                    }
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("", "Success!", "Ok");
            }

        }
    }
}