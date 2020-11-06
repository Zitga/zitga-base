using Debug = UnityEngine.Debug;
using System;
using System.Text;

namespace ZitgaPackageManager
{
    public static class ZLog
    {
        public static bool IsTest = true;

        private static string dataPath = "./Log_empire.txt";
        private static StringBuilder builder = new StringBuilder();

        [System.Diagnostics.Conditional("E_LOG")]
        public static void Info(object content)
        {
            if (IsTest)
            {
#if UNITY_STANDALONE && !UNITY_EDITOR
            DateTime now = DateTime.Now;
            builder.Append(string.Format("[{0} {1}] {2}{3}", now.ToShortDateString(), now.ToShortTimeString(), content, Environment.NewLine));
#else
                Debug.Log(content);
#endif
            }
        }

        [System.Diagnostics.Conditional("E_LOG")]
        public static void Warning(object content)
        {
            if (IsTest)
            {
#if UNITY_STANDALONE && !UNITY_EDITOR
            DateTime now = DateTime.Now;
            builder.Append(string.Format("[{0} {1}] {2}{3}", now.ToShortDateString(), now.ToShortTimeString(), content, Environment.NewLine));
#else
                //Debug.LogWarning(content);
#endif
            }
        }

        [System.Diagnostics.Conditional("E_LOG")]
        public static void Error(object content)
        {
            if (IsTest)
            {
#if UNITY_STANDALONE && !UNITY_EDITOR
            DateTime now = DateTime.Now;
            builder.Append(string.Format("[{0} {1}] {2}{3}", now.ToShortDateString(), now.ToShortTimeString(), content, Environment.NewLine));
#else
                Debug.LogError(content);
#endif
            }
        }

        [System.Diagnostics.Conditional("E_LOG")]
        public static void Exception(Exception e)
        {
            if (IsTest)
            {
#if UNITY_STANDALONE && !UNITY_EDITOR
            DateTime now = DateTime.Now;
            builder.Append(string.Format("[{0} {1}] {2}{3}", now.ToShortDateString(), now.ToShortTimeString(), StringUtils.FlattenException(e), Environment.NewLine));
#else
                Debug.LogException(e);
#endif
            }
        }

        public static void FlushToLogFile()
        {
            if (builder.Length > 0)
            {
                System.IO.File.AppendAllText(dataPath, builder.ToString());
                builder = new StringBuilder();
            }
        }

        public static void LogNull<T>(T t)
        {
            if (t == null)
            {
                Error(t);
            }
        }
    }
}