namespace Zitga.CsvTools
{

// ---------------
//  String => Int
// ---------------
    [UnityEditor.CustomPropertyDrawer(typeof(StringIntDictionary))]
    public class StringIntDictionaryDrawer : SerializableDictionaryDrawer<string, int>
    {
        protected override SerializableKeyValueTemplate<string, int> GetTemplate()
        {
            return GetGenericTemplate<SerializableStringIntTemplate>();
        }
    }

    internal class SerializableStringIntTemplate : SerializableKeyValueTemplate<string, int>
    {
    }
    
    // ---------------
//  String => String
// ---------------
    [UnityEditor.CustomPropertyDrawer(typeof(StringStringDictionary))]
    public class StringStringDictionaryDrawer : SerializableDictionaryDrawer<string, string>
    {
        protected override SerializableKeyValueTemplate<string, string> GetTemplate()
        {
            return GetGenericTemplate<SerializableStringStringTemplate>();
        }
    }

    internal class SerializableStringStringTemplate : SerializableKeyValueTemplate<string, string>
    {
    }
}