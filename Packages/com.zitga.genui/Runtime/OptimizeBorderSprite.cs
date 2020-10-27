using UnityEngine;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;

namespace GenUI
{
    public class OptimizeBorderSprite
    {
        #region Editor
        [MenuItem("Assets/GenUI/Texture/OptimizeSprite")]
        private static void OptimizeSprite()
        {
            if (!CheckNameConvention())
                return;

            List<Sprite> sprites = new List<Sprite>();
            foreach (var obj in Selection.objects)
            {
                Sprite sprite = null;
                if (obj is Sprite)
                {
                    sprite = obj as Sprite;
                }
                else if (obj is Texture2D)
                {
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(obj));
                }
                else
                {
                    Debug.LogError("Not support type " + obj.GetType().ToString());
                    continue;
                }

                if (sprite != null && ((sprite.border.x > 0 && sprite.border.z > 0) || (sprite.border.y > 0 && sprite.border.w > 0)))
                {
                    string path = AssetDatabase.GetAssetPath(sprite.texture);
                    int pixelX = sprite.texture.width;
                    int pixelY = sprite.texture.height;
                    int width = pixelX;
                    int height = pixelY;
                    int bottom = 0;
                    int left = 0;
                    if (sprite.border.x > 0 && sprite.border.z > 0)
                    {
                        left = (int)sprite.border.x;
                        width = (int)(sprite.border.x + sprite.border.z);
                    }
                    if (sprite.border.y > 0 && sprite.border.w > 0)
                    {
                        bottom = (int)sprite.border.y;
                        height = (int)(sprite.border.y + sprite.border.w);
                    }
                    Texture2D texture = new Texture2D(width, height);
                    int size = width * height;
                    TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (!ti.isReadable)
                    {
                        ti.isReadable = true;
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                    Color[] pixel = sprite.texture.GetPixels(0, 0, pixelX, pixelY);
                    Color[] pixelNew = new Color[size];
                    int c, r;
                    for (int i = 0; i < size; i++)
                    {
                        c = i % width;
                        r = i / width;
                        if (c >= left)
                        {
                            c = pixelX - (width - c);
                        }
                        if (r >= bottom)
                        {
                            r = pixelY - (height - r);
                        }
                        pixelNew[i] = pixel[r * pixelX + c];
                    }
                    texture.SetPixels(pixelNew);
                    string pathSystem = Application.dataPath;
                    pathSystem = pathSystem.Substring(0, pathSystem.Length - 6) + AssetDatabase.GetAssetPath(sprite);
                    texture.Apply();
                    byte[] bytes = texture.EncodeToPNG();
                    File.WriteAllBytes(pathSystem, bytes);
                    sprites.Add(sprite);
                    if (ti.isReadable == true)
                    {
                        ti.isReadable = false;
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                }
            }
            AssetDatabase.Refresh();
            if (sprites.Count > 0)
            {
                FixImageTypeToSliced();
            }

            Debug.Log("***Optimize Done***");
        }

        [MenuItem("Assets/GenUI/Texture/FixImageTypeToSliced")]
        private static void FixImageTypeToSliced()
        {
            var referenceCache = new Dictionary<string, List<string>>();
            List<string> spriteNames = new List<string>();
            List<GameObject> references = new List<GameObject>();
            List<string> paths = new List<string>();

            foreach (Object child in Selection.objects)
            {
                if (child != null)
                {
                    spriteNames.Add(child.name);
                }
            }

            if (spriteNames.Count == 0)
            {
                Debug.LogError("List sprites is empty!");
                return;
            }

            GetFilePaths("Assets", ".prefab", ref paths);

            //
            // Loop through all files, and add any that have the selected object in it's list of dependencies
            //
            Object[] tmpArray = new Object[1];
            references.Clear();

            foreach (Object child in Selection.objects)
            {
                for (int i = 0; i < paths.Count; ++i)
                {
                    tmpArray[0] = AssetDatabase.LoadMainAssetAtPath(paths[i]);
                    if (tmpArray != null && tmpArray.Length > 0 && tmpArray[0] != child) // Don't add self
                    {
                        Object[] dependencies = EditorUtility.CollectDependencies(tmpArray);
                        if (System.Array.Exists(dependencies, item => item == child))
                        {
                            // Don't add if another of the dependencies is already in there
                            references.Add(tmpArray[0] as GameObject);
                        }

                    }
                }
            }

            //
            // Go through the references, get dependencies of each and remove any that have another dependency on the match list. We only want direct dependencies.
            //
            for (int i = references.Count - 1; i >= 0; i--)
            {
                tmpArray[0] = references[i];
                Object[] dependencies = EditorUtility.CollectDependencies(tmpArray);

                bool shouldRemove = false;

                for (int j = 0; j < dependencies.Length && shouldRemove == false; ++j)
                {
                    Object dependency = dependencies[j];
                    shouldRemove = (references.Find(item => item == dependency && item != tmpArray[0]) != null);
                }

                if (shouldRemove)
                    references.RemoveAt(i);
            }

            List<Image> imgs;
            foreach (GameObject gm in references)
            {
                imgs = GetImage(gm.transform, spriteNames);
                foreach (Image im in imgs)
                {
                    if (im.type == Image.Type.Simple)
                    {
                        im.type = Image.Type.Sliced;
                    }
                }
                imgs.Clear();
            }
            references.Clear();
            //referenceCache.Clear();

            Debug.Log("***Fix Done***");
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCBottomRight")]
        public static void FixSupportETCBottomRight()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorBottonRight, true);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCTopRight")]
        public static void FixSupportETCTopRight()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorTopRight);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCTopLeft")]
        public static void FixSupportETCTopLeft()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorTopLeft);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCBottomLeft")]
        public static void FixSupportETCBottomLeft()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorBottomLeft);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCMid")]
        public static void FixSupportETCMid()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorMid);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCMidBottom")]
        public static void FixSupportETCMidBottom()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorMidBottom);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCMidTop")]
        public static void FixSupportETCMidTop()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorMidTop);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCMidLeft")]
        public static void FixSupportETCMidLeft()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorMidLeft);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/FixSupportETCMidRight")]
        public static void FixSupportETCMidRight()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    FixSupportETCTexure((Texture2D)obj, SetColorMidRight);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/GenUI/Texture/DisableReadWriteTexture")]
        public static void DisableReadWriteTexture()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is Texture2D)
                {
                    DisableReadWrite((Texture2D)obj);
                }
            }
            AssetDatabase.Refresh();
        }
        #endregion

        #region Support
        public static List<Image> GetImage(Transform trans, List<string> spriteNames)
        {
            var result = new List<Image>();
            bool active = trans.gameObject.activeSelf;
            trans.gameObject.SetActive(true);
            var im = trans.GetComponent<Image>();
            var allIm = trans.GetComponentsInChildren<Image>();

            if (im != null && im.sprite != null && spriteNames.Equals(im.sprite.name))
            {
                result.Add(im);
            }
            foreach (Image item in allIm)
            {
                if (item != null && item.sprite != null && spriteNames.Contains(item.sprite.name))
                    result.Add(item);
            }

            trans.gameObject.SetActive(active);
            return result;
        }

        //[MenuItem("Assets/GenUI/FindTexturePVRTC")]
        static void FindTexturePVRTC()
        {
            if (Selection.activeObject != null)
            {
                string[] guids2 = AssetDatabase.FindAssets("t:texture2D", new[] { AssetDatabase.GetAssetPath(Selection.activeObject) });
                List<Object> gos = new List<Object>();
                foreach (string guid2 in guids2)
                {
                    if (AssetDatabase.GUIDToAssetPath(guid2).Contains("/PVRTC/"))
                    {
                        Object go = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid2));
                        gos.Add(go);
                    }
                }
                Selection.objects = gos.ToArray();
            }
        }

        //[MenuItem("Assets/GenUI/Texture/FindTextureASTC")]
        static void FindTextureASTC()
        {
            Debug.Log("FindTextureASTC");

            string[] guids2 = AssetDatabase.FindAssets("t:texture2D", new[] { "Assets/AssetBundles/SpineModels/UI" });
            Debug.Log(guids2.Length);
            List<Object> gos = new List<Object>();
            foreach (string guid2 in guids2)
            {
                if (!AssetDatabase.GUIDToAssetPath(guid2).Contains("/PVRTC/"))
                {
                    Object go = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid2));
                    gos.Add(go);
                }
            }
            Debug.Log(gos.Count);
            Selection.objects = gos.ToArray();

        }


        public static void SetColor(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY, int left, int bottom)
        {
            if (y < bottom || x < left || x >= pixelX + left || y >= pixelY + bottom)
            {
                pixelNew[index] = new Color(0, 0, 0, 0);
            }
            else
            {
                pixelNew[index] = pixel[(y - bottom) * pixelX + x - left];
            }
        }

        public static void SetColorBottonRight(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, 0, deltaY);
        }

        public static void SetColorTopRight(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, 0, 0);
        }

        public static void SetColorTopLeft(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, deltaX, 0);
        }

        public static void SetColorBottomLeft(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, deltaX, deltaY);
        }

        public static void SetColorMid(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, Mathf.FloorToInt(deltaX / 2), Mathf.FloorToInt(deltaY / 2));
        }

        public static void SetColorMidTop(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, Mathf.FloorToInt(deltaX / 2), 0);
        }

        public static void SetColorMidBottom(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, Mathf.FloorToInt(deltaX / 2), deltaY);
        }

        public static void SetColorMidLeft(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, deltaX, Mathf.FloorToInt(deltaY / 2));
        }

        public static void SetColorMidRight(int index, Color[] pixelNew, Color[] pixel, int x, int y, int pixelX, int pixelY, int deltaX, int deltaY)
        {
            SetColor(index, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY, 0, Mathf.FloorToInt(deltaY / 2));
        }

        public static void FixSupportETCTexure(Texture2D t, System.Action<int, Color[], Color[], int, int, int, int, int, int> setColor, bool isFixAtlas = false)
        {
            int delta = 4;
            string path = AssetDatabase.GetAssetPath(t);
            int pixelX = t.width;
            int pixelY = t.height;
            int deltaX = (delta - t.width % delta) % delta;
            int deltaY = (delta - t.height % delta) % delta;
            if (deltaX != 0 || deltaY != 0)
            {
                int width = pixelX + deltaX;
                int height = pixelY + deltaY;

                Texture2D texture = new Texture2D(width, height);
                int size = width * height;
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null && !ti.isReadable)
                {
                    ti.isReadable = true;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                Color[] pixel = t.GetPixels(0, 0, pixelX, pixelY);
                Color[] pixelNew = new Color[size];
                int x, y;
                for (int i = 0; i < size; i++)
                {
                    y = Mathf.FloorToInt(i / width);
                    x = i % width;
                    setColor(i, pixelNew, pixel, x, y, pixelX, pixelY, deltaX, deltaY);
                }
                texture.SetPixels(pixelNew);
                string pathSystem = Application.dataPath;
                pathSystem = pathSystem.Substring(0, pathSystem.Length - 6) + AssetDatabase.GetAssetPath(t);
                texture.Apply();
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(pathSystem, bytes);
                if (ti != null && ti.isReadable == true)
                {
                    ti.isReadable = false;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }

                if (isFixAtlas == true)
                {
                    string pathAtlas = pathSystem.Substring(0, pathSystem.Length - 3) + "atlas.txt";
                    if (File.Exists(pathAtlas))
                    {
                        string aslat = File.ReadAllText(pathAtlas);
                        string format = "size: {0},{1}";
                        aslat = aslat.Replace(string.Format(format, pixelX, pixelY), string.Format(format, width, height));
                        File.WriteAllText(pathAtlas, aslat);
                    }
                }
            }
        }

        public static void DisableReadWrite(Texture2D t)
        {
            string path = AssetDatabase.GetAssetPath(t);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && ti.isReadable == true)
            {
                ti.isReadable = false;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        static void GetFilePaths(string startingDirectory, string extention, ref List<string> paths)
        {
            try
            {
                // Add any file paths with the correct extention
                string[] files = Directory.GetFiles(startingDirectory);
                for (int i = 0; i < files.Length; ++i)
                {
                    string file = files[i];
                    if (file.EndsWith(extention))
                    {
                        paths.Add(file);
                    }
                }

                // Recurse for all directories
                string[] directories = Directory.GetDirectories(startingDirectory);
                for (int i = 0; i < directories.Length; ++i)
                {
                    GetFilePaths(directories[i], extention, ref paths);
                }
            }
            catch (System.Exception excpt)
            {
                Debug.LogError(excpt.Message);
            }
        }

        public static bool CheckNameConvention()
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
                return false;
            }
            return true;
        }
        #endregion
    }
}
