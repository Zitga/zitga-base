using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZitgaPackageManager;

public class ZBasePackageIdConfig
{
    public static readonly string Repo = "zitga-base";
    public static readonly string NamePackageManager = "com.zitga.zitgapackagemanager";
    public static readonly string NameDependencyManager = "com.google.external-dependency-manager";
    public static readonly ScopedRegistry ScopesGoogle = new ScopedRegistry("Game Package Registry by Google", "https://unityregistry-pa.googleapis.com", new string[] { "com.google" });

    public static Dictionary<string, string> ListPackages = new Dictionary<string, string>();
}
