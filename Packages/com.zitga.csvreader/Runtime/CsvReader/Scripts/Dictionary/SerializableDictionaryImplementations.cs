using System;
 
namespace Zitga.CsvTools
{
    // ---------------
//  String => Int
// ---------------
    [Serializable]
    public class StringIntDictionary : SerializableDictionary<string, int> {}

// ---------------
//  String => String
// ---------------
    [Serializable]
    public class StringStringDictionary : SerializableDictionary<string, string> {}
}



