using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZitgaPackageManager
{
    [System.Serializable]
    public class ScopedRegistry
    {
        public string name;
        public string url;
        public string[] scopes = new string[0];

        public ScopedRegistry()
        {

        }

        public ScopedRegistry(string name, string url, string[] scopes)
        {
            this.name = name;
            this.url = url;
            this.scopes = scopes;
        }
    }
}
