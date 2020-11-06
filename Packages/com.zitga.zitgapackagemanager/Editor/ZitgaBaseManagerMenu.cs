using System.Text;
using UnityEditor;
using UnityEngine;

namespace ZitgaPackageManager.Editors
{
    public class ZitgaBaseManagerMenu
    {
        [MenuItem("ZitgaBase/Tool Manager", false, 0)]
        public static void ToolManager()
        {
            ZBaseDependenciesManager.ShowZBaseDependenciesManager();
        }

        [MenuItem("ZitgaBase/Tool Debug/Show Log", false, 0)]
        public static void EnableToolDebug()
        {
            BuildTargetGroup buildTarget = BuildTargetGroup.Standalone;
#if UNITY_ANDROID
            buildTarget = BuildTargetGroup.Android;
#elif UNITY_IOS
            buildTarget = BuildTargetGroup.iOS;
#endif

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

            if (!defines.Contains("E_LOG"))
            {
                if (defines.Length <= 0)
                {
                    defines = "E_LOG";
                }
                else
                {
                    defines = defines + ";" + "E_LOG";
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
            }
        }

        [MenuItem("ZitgaBase/Tool Debug/Hide Log", false, 0)]
        public static void DisableToolDebug()
        {
            BuildTargetGroup buildTarget = BuildTargetGroup.Standalone;
#if UNITY_ANDROID
            buildTarget = BuildTargetGroup.Android;
#elif UNITY_IOS
            buildTarget = BuildTargetGroup.iOS;
#endif

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
            StringBuilder newDefines = new StringBuilder();

            if (defines.Contains("E_LOG"))
            {
                string[] arrDefine = defines.Split(';');
                if (arrDefine != null)
                {
                    if (arrDefine.Length == 1)
                    {
                        defines = string.Empty;
                    }
                    else
                    {
                        foreach (var item in arrDefine)
                        {
                            if (item.Equals("E_LOG"))
                            {
                                continue;
                            }

                            newDefines.Append(item).Append(";");
                        }

                        defines = newDefines.ToString();
                    }
                }


                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
            }
        }
    }
}