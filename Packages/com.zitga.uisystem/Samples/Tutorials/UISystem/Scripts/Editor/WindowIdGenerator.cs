using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Loxodon.Framework.Views;
using UnityEditor;
using UnityEngine;

namespace Loxodon.UIFramework.Editor
{
    /// <summary>
    /// It's way less error prone to have constants for your UI instead of directly typing in ids.
    /// This utility searches through a folder for Window prefabs and creates a class with Window Id constants
    /// based on their names. That way you can do eg:
    ///
    /// UIFrame.OpenWindow(WindowIds.ItsAWindow);
    ///
    /// instead of wondering for ages why
    ///
    /// UIFrame.OpenWindow("ltsAWindow");
    ///
    /// isn't working. It isn't a perfect solution, however: if you rename prefabs, you may have to refactor part
    /// of your code. You could also manually define these and use an asset postprocessor to validate it and warn
    /// if the data is stale, or just do it manually. The solution below is my current favourite local maximum.
    /// </summary>
    public class WindowIdProcessor : AssetPostprocessor
    {
        private const string UIPrefabFolder = "Assets/Zitga/Samples/UISystem/Resources/UI";
        private const string UIIdScriptFolder = "Assets/Zitga/Samples/Scripts";
        private const string WindowIdScriptName = "WindowIds";
        private const string WindowIdScriptNamespace = "Loxodon.Framework.Examples";

        [MenuItem("Tools/Generate/Re-generate UI WindowIds")]
        public static void RegenerateWindowIdsAndRefresh() {
            RegenerateWindowIds(true);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths) {
            foreach (string str in importedAssets) {
                if (str.Contains(UIPrefabFolder)) {
                    RegenerateWindowIds(true);
                    return;
                }
            }

            foreach (string str in deletedAssets) {
                if (str.Contains(UIPrefabFolder)) {
                    RegenerateWindowIds(true);
                    return;
                }
            }

            for (int i = 0; i < movedAssets.Length; i++) {
                if (movedAssets[i].Contains(UIPrefabFolder)
                    || movedFromAssetPaths[i].Contains(UIPrefabFolder)) {
                    RegenerateWindowIds(true);
                    return;
                }
            }
        }

        /// <summary>
        /// Get real path to load in game. Start from Resources/...
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string GetPrefabPath(string path)
        {
            const string firstString = "Resources/";
            const string lastString = ".prefab";
            var pos1 = path.IndexOf(firstString, StringComparison.Ordinal) + firstString.Length;
            var pos2 = path.Substring(pos1).IndexOf(lastString, StringComparison.Ordinal);
            return path.Substring(pos1, pos2);
        }

        private static void RegenerateWindowIds(bool refreshAssetDatabase) {
            var paths = new Dictionary<string, string>();
            var assets = AssetDatabase.FindAssets("t:prefab", new[] {UIPrefabFolder});
            foreach (var asset in assets) {
                string path = AssetDatabase.GUIDToAssetPath(asset);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var windowController = go.GetComponent<IWindow>();
                var name = GetPrefabPath(path).Replace(" ", string.Empty);
                if (string.Empty.Equals(name))
                {
                    Debug.LogErrorFormat("Prefab is invalid: {0}", path);
                    continue;
                }
                if (windowController != null) {
                    if (paths.ContainsKey(name)) {
                        Debug.LogError(
                            $"You have multiple window prefabs with the same name: {name}! Locations: (1){paths[name]}, (2){path}");
                    }
                    else if (path.IndexOf("Resources", StringComparison.Ordinal) == -1)
                    {
                        Debug.LogError(
                            $"You need to put file in the Resources folders: {path}");
                    } 
                    else 
                    {
                        paths.Add(name, path);
                        Debug.Log($"Registering {path} as {name}");
                    }
                }
            }

            var scripts = AssetDatabase.FindAssets($"t:script {WindowIdScriptName}", new[] {UIIdScriptFolder});
            if (scripts.Length > 0) {
                string filePath = AssetDatabase.GUIDToAssetPath(scripts[0]);
                WriteIdClass(paths, filePath);
                if (refreshAssetDatabase) {
                    AssetDatabase.Refresh();
                }
            }
            else {
                Debug.LogError("Could not find WindowIds script file! Create the file and try again.");
            }
        }

        private static void WriteIdClass(Dictionary<string, string> idPaths, string filePath) {
            var targetUnit = new CodeCompileUnit();
            var codeNamespace = new CodeNamespace(WindowIdScriptNamespace);
            var targetClass = new CodeTypeDeclaration(WindowIdScriptName) {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
            };

            codeNamespace.Types.Add(targetClass);
            targetUnit.Namespaces.Add(codeNamespace);

            foreach (var idPathPair in idPaths) {
                var idField = new CodeMemberField(typeof(string), idPathPair.Key.Replace("/", string.Empty)) {
                    Attributes = MemberAttributes.Public | MemberAttributes.Const,
                    InitExpression = new CodePrimitiveExpression(idPathPair.Key)
                };

                targetClass.Members.Add(idField);
            }

            GenerateCSharpCode(targetUnit, filePath);
        }

        private static void GenerateCSharpCode(CodeCompileUnit targetUnit, string fileName) {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var options = new CodeGeneratorOptions();

            using (var sourceWriter = new StreamWriter(fileName)) {
                provider.GenerateCodeFromCompileUnit(targetUnit, sourceWriter, options);
            }
        }
    }
}
