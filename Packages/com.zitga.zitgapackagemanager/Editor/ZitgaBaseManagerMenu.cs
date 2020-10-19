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
    }
}