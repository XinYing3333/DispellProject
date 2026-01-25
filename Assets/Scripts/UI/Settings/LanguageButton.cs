using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class LanguageButton : MonoBehaviour
{
    public enum Language
    {
        English = 0,
        ChineseTraditional = 1
    }

    [Header("Auto Wire")]
    [SerializeField] private Button targetButton;           // 不填：自動抓本物件 Button
    [SerializeField] private TextMeshProUGUI valueText;     // 右側顯示 EN / 繁中（可不填）

    [Header("State")]
    [SerializeField] private Language currentLanguage = Language.English;

    private const string LANG_KEY = "LANGUAGE"; // 存檔 key

    private void Awake()
    {
        if (!targetButton) targetButton = GetComponent<Button>();
        if (!valueText) valueText = GetComponentInChildren<TextMeshProUGUI>(true);

        targetButton.onClick.RemoveListener(OnClickCycle);
        targetButton.onClick.AddListener(OnClickCycle);

        Load();
        RefreshText();
    }

    private void OnDestroy()
    {
        if (targetButton)
            targetButton.onClick.RemoveListener(OnClickCycle);
    }

    private void OnClickCycle()
    {
        currentLanguage = (Language)(((int)currentLanguage + 1) % 2);
        Save();
        Apply();
        RefreshText();
    }

    private void RefreshText()
    {
        if (!valueText) return;

        valueText.text = currentLanguage switch
        {
            Language.English => "English",
            Language.ChineseTraditional => "繁體中文",
            _ => "English"
        };
    }

    private void Apply()
    {
        // ★ 這裡只負責「通知語言改變」
        // 你之後的多語系系統（TMP/Ink/CSV）都從這裡接
        Debug.Log("Language set to: " + currentLanguage);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(LANG_KEY, (int)currentLanguage);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        currentLanguage = (Language)PlayerPrefs.GetInt(LANG_KEY, 0);
    }

    public Language GetCurrentLanguage()
    {
        return currentLanguage;
    }
}