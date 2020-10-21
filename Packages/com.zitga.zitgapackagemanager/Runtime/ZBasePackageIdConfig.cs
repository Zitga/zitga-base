using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZBasePackageIdConfig
{
    public static readonly string Repo = "zitga-base";
    public static readonly string NamePackageManager = "com.zitga.zitgapackagemanager";
    public static readonly Dictionary<string, string> ListPackages = new Dictionary<string, string>() {
        { "com.zitga.zitgapackagemanager", "Zitga Package Manager" },
        { "com.vovgou.loxodon-framework","Loxodon Framework" },
        { "com.cysharp.unitask","UniTask"},
        { "DOTween_Pro_1.0.244.unitypackage","DOTween Pro v1.0.244" },
        { "Clean_Empty_Directories.unitypackage","Clean Empty Directories"}
    };
}
