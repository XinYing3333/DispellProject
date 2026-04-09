using UnityEngine;

[System.Serializable]
public struct SpellUIEntry
{
    public SpellType type;
    public Sprite icon;
    public string ENName;
    public string CHName;

    public string GetLocalizedName(UI.Localization.Language lang)
    {
        return lang switch
        {
            UI.Localization.Language.zh => CHName,
            UI.Localization.Language.en => ENName,
            _ => !string.IsNullOrEmpty(ENName) ? ENName : CHName
        };
    }
}