using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ZUnityIAP
{
    public class ZUnityIAPEditor
    {
        [MenuItem("ZitgaBase/Unity IAP Config", false, 1)]
        public static void ConfigDefineForUnityIAP()
        {
            BuildTargetGroup buildTarget = BuildTargetGroup.Standalone;
#if UNITY_ANDROID
            buildTarget = BuildTargetGroup.Android;
#elif UNITY_IOS
                buildTarget = BuildTargetGroup.iOS;
#endif

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

            if (!defines.Contains("UNITY_IAP"))
            {
                if (defines.Length <= 0)
                {
                    defines = "UNITY_IAP";
                }
                else
                {
                    defines += ";UNITY_IAP";
                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, defines);
            }
        }
    }
}