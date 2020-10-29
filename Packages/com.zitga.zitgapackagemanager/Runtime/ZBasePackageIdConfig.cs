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
        { "zitga.dotweenpro","DOTween_Pro_1.0.244" },
        { "zitga.clearnemptydirectories","Clean_Empty_Directories"},
        { "com.zitga.genui","GenUI" },
        { "zitga.externaldependencyunity","External_Dependency_Manager_1.2.161" },
        { "zitga.logviewer","Log_Viewer_1.8.0" },
        { "zitga.facebook","Facebook_Unity_Sdk_8.1.0" },
    };
}
