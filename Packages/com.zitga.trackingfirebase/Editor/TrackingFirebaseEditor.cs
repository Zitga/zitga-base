using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Zitga.TrackingFirebase.Editor
{
    public class TrackingFirebaseEditor
    {
        const string DEFINE_FIREBASE = "TRACKING_FIREBASE";

        [MenuItem("ZitgaBase/Tracking Firebase/Enable Tracking", false, 1)]
        public static void EnableTracking()
        {
            BuildTargetGroup buildTarget = BuildTargetGroup.Standalone;
#if UNITY_ANDROID
            buildTarget = BuildTargetGroup.Android;
#elif UNITY_IOS
            buildTarget = BuildTargetGroup.iOS;
#endif

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

            if (!defines.Contains(DEFINE_FIREBASE))
            {
                if (defines.Length <= 0)
                {
                    defines = DEFINE_FIREBASE;
                }
                else
                {
                    defines = defines + ";" + DEFINE_FIREBASE;
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
            }
        }

        [MenuItem("ZitgaBase/Tracking Firebase/Disable Tracking", false, 2)]
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

            if (defines.Contains(DEFINE_FIREBASE))
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
                            if (item.Equals(DEFINE_FIREBASE))
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