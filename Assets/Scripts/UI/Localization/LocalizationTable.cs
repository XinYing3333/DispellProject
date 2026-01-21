using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Localization/Table")]
public class LocalizationTable : ScriptableObject
{
    public SystemLanguage language;

    [Serializable]
    public struct Entry
    {
        public string key;
        [TextArea] public string value;
    }

    public Entry[] entries;
}
