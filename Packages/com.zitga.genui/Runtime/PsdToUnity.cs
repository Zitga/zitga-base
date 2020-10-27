using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace GenUI
{
    public class PsdToUnity
    {
        struct SkinData
        {
            public string[] bone;
            public Vector2 pos;
            public Vector2 rect;
        }

        [MenuItem("Assets/GenUI/FromPSD/PsdToUnity")]
        private static void ImportPsdAsset()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var pathFolder = "";
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '/')
                {
                    pathFolder = path.Substring(0, i + 1);
                    break;
                }
            }
            if (path.ToLower().EndsWith(".csv"))
            {
                string dataPosition = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                var lines = dataPosition.Split('\n', '\r');

                List<SkinData> skinData = new List<SkinData>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line) == false)
                    {
                        var datas = line.Split(',');
                        if (datas.Length != 5)
                        {
                            Debug.Log("Line is not valid: " + line);
                        }
                        else
                        {
                            skinData.Add(new SkinData() { bone = datas[0].Split('/'), pos = new Vector2(float.Parse(datas[1]), float.Parse(datas[2])), rect = new Vector2(float.Parse(datas[3]), float.Parse(datas[4])) });
                        }
                    }
                }
                if (skinData.Count > 0)
                {
                    Canvas canvas = GameObject.FindObjectOfType<Canvas>();
                    if (canvas == null)
                    {
                        Debug.LogError("Null Canvas");
                        return;
                    }
                    else
                    {
                        Transform root = (new GameObject("root")).transform;
                        root = root.gameObject.AddComponent<RectTransform>();
                        root.SetParent(canvas.transform);
                        root.localPosition = Vector3.zero;
                        ((RectTransform)root).anchorMin = Vector2.zero;
                        ((RectTransform)root).anchorMax = Vector2.one;
                        ((RectTransform)root).sizeDelta = Vector2.zero;
                        for (int i = skinData.Count - 1; i >= 0; i--)
                        {
                            SkinData skin = skinData[i];
                            Transform parent = root;
                            for (int j = 1; j < skin.bone.Length; j++)
                            {
                                Transform currentBone = parent.Find(skin.bone[j]);
                                if (currentBone == null)
                                {
                                    currentBone = (new GameObject(skin.bone[j])).transform;
                                    if (currentBone.GetComponent<RectTransform>() == null)
                                    {
                                        currentBone = currentBone.gameObject.AddComponent<RectTransform>();
                                        ((RectTransform)currentBone).sizeDelta = Vector2.zero;
                                    }
                                    currentBone.position = root.position;
                                    currentBone.SetParent(parent);
                                }
                                parent = currentBone;
                            }
                            Image image = parent.gameObject.GetComponent<Image>();
                            if (image == null)
                            {
                                image = parent.gameObject.AddComponent<Image>();
                            }
                            string n = skin.bone[skin.bone.Length - 1];
                            for (int j = n.Length - 2; j >= 0; j--)
                            {
                                if (n[j] == '[')
                                {
                                    n = n.Substring(0, j);
                                    break;
                                }
                            }
                            string pathSprite = pathFolder + n + ".png";
                            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pathSprite);
                            if (image != null && sprite != null)
                            {
                                image.sprite = sprite;
                                image.rectTransform.sizeDelta = skin.rect;
                            }
                            if (image != null)
                            {
                                image.rectTransform.position = new Vector3(skin.pos.x, skin.pos.y, 0);
                                image.raycastTarget = false;
                            }
                        }

                        Transform[] childs = root.GetComponentsInChildren<Transform>();
                        for (int i = 1; i < childs.Length; i++)
                        {
                            string n = childs[i].name;
                            for (int j = n.Length - 2; j >= 0; j--)
                            {
                                if (n[j] == '[')
                                {
                                    childs[i].name = n.Substring(0, j);
                                    break;
                                }
                            }
                        }

                        EditorGUIUtility.PingObject(root.gameObject);
                    }
                }
            }
        }

        [MenuItem("Assets/GenUI/FromPSD/PsdToUnityOptimaze")]
        private static void ImportPsdAssetOptimaze()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var pathFolder = "";
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '/')
                {
                    pathFolder = path.Substring(0, i + 1);
                    break;
                }
            }
            if (path.ToLower().EndsWith(".csv"))
            {
                string dataPosition = AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
                var lines = dataPosition.Split('\n');

                List<SkinData> skinData = new List<SkinData>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line) == false)
                    {
                        var datas = line.Split(',');
                        if (datas.Length != 5)
                        {
                            Debug.Log("Line is not valid: " + line);
                        }
                        else
                        {
                            skinData.Add(new SkinData() { bone = datas[0].Split('/'), pos = new Vector2(float.Parse(datas[1]), float.Parse(datas[2])), rect = new Vector2(float.Parse(datas[3]), float.Parse(datas[4])) });
                        }
                    }
                }
                if (skinData.Count > 0)
                {
                    Canvas canvas = GameObject.FindObjectOfType<Canvas>();
                    if (canvas == null)
                    {
                        Debug.LogError("Null Canvas");
                        return;
                    }
                    else
                    {
                        Transform root = (new GameObject("root")).transform;
                        root = root.gameObject.AddComponent<RectTransform>();
                        root.SetParent(canvas.transform);
                        root.localPosition = Vector3.zero;
                        ((RectTransform)root).anchorMin = Vector2.zero;
                        ((RectTransform)root).anchorMax = Vector2.one;
                        ((RectTransform)root).sizeDelta = Vector2.zero;
                        for (int i = skinData.Count - 1; i >= 0; i--)
                        {
                            SkinData skin = skinData[i];
                            RectTransform rectTransform = (new GameObject(skin.bone[skin.bone.Length - 1])).AddComponent<RectTransform>();
                            rectTransform.SetParent(root);
                            Image image = rectTransform.gameObject.AddComponent<Image>();

                            string n = skin.bone[skin.bone.Length - 1];
                            for (int j = n.Length - 2; j >= 0; j--)
                            {
                                if (n[j] == '[')
                                {
                                    n = n.Substring(0, j);
                                    break;
                                }
                            }
                            string pathSprite = pathFolder + n + ".png";
                            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pathSprite);
                            if (image != null && sprite != null)
                            {
                                image.sprite = sprite;
                                image.rectTransform.sizeDelta = skin.rect;
                            }
                            if (image != null)
                            {
                                image.rectTransform.position = new Vector3(skin.pos.x, skin.pos.y, 0);
                                image.raycastTarget = false;
                            }
                        }

                        Transform[] childs = root.GetComponentsInChildren<Transform>();
                        for (int i = 1; i < childs.Length; i++)
                        {
                            string n = childs[i].name;
                            for (int j = n.Length - 2; j >= 0; j--)
                            {
                                if (n[j] == '[')
                                {
                                    childs[i].name = n.Substring(0, j);
                                    break;
                                }
                            }
                        }

                        EditorGUIUtility.PingObject(root.gameObject);
                    }
                }
            }
        }       
    }
}