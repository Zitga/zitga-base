using System.Text;
using UnityEditor;

namespace Zitga.TrackingAppsflyer.Editor
{
    public class TrackingAppsflyerEditor
    {
        const string DEFINE_APPSFLYER = "TRACKING_APPSFLYER";

        [MenuItem("ZitgaBase/Tracking Appsflyer/Enable Tracking", false, 9)]
        public static void EnableTracking()
        {
            BuildTargetGroup buildTarget = BuildTargetGroup.Standalone;
#if UNITY_ANDROID
            buildTarget = BuildTargetGroup.Android;
#elif UNITY_IOS
            buildTarget = BuildTargetGroup.iOS;
#endif

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

            if (!defines.Contains(DEFINE_APPSFLYER))
            {
                if (defines.Length <= 0)
                {
                    defines = DEFINE_APPSFLYER;
                }
                else
                {
                    defines = defines + ";" + DEFINE_APPSFLYER;
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
            }
        }

        [MenuItem("ZitgaBase/Tracking Appsflyer/Disable Tracking", false, 10)]
        public static void DisableTracking()
        {
            BuildTargetGroup buildTarget = BuildTargetGroup.Standalone;
#if UNITY_ANDROID
            buildTarget = BuildTargetGroup.Android;
#elif UNITY_IOS
            buildTarget = BuildTargetGroup.iOS;
#endif

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
            StringBuilder newDefines = new StringBuilder();

            if (defines.Contains(DEFINE_APPSFLYER))
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
                            if (item.Equals(DEFINE_APPSFLYER))
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