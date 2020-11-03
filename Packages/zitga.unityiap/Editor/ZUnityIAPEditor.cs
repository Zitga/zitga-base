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
            if (Application.platform == RuntimePlatform.Android)
            {
                buildTarget = BuildTargetGroup.Android;
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                buildTarget = BuildTargetGroup.iOS;
            }

            string currDefine = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);

            if (currDefine.Length <= 0)
            {
                currDefine = "UNITY_IAP";
            }
            else
            {
                currDefine += ";UNITY_IAP";
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, currDefine);
        }
    }
}