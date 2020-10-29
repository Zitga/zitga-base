using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZBasePackageIdConfig
{
    public static readonly string Repo = "zitga-base";
    public static readonly string NamePackageManager = "com.zitga.zitgapackagemanager";
    public static readonly string NameDependencyManager = "com.google.external-dependency-manager";
    public static readonly Dictionary<string, string> ListPackages = new Dictionary<string, string>() {
        { "com.zitga.zitgapackagemanager", "Zitga Package Manager" },
        { "com.vovgou.loxodon-framework","Loxodon Framework" },
        { "com.cysharp.unitask","UniTask"},
        { "zitga.dotweenpro","DOTween_Pro" },
        { "zitga.clearnemptydirectories","Clean_Empty_Directories"},
        { "com.zitga.genui","GenUI" },
        { "zitga.externaldependencyunity","External_Dependency_Manager" },
        { "zitga.logviewer","Log_Viewer" },
        { "zitga.facebook","Facebook_Unity_Sdk" },
    };
}
