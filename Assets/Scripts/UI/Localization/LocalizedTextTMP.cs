using TMPro;
using UnityEngine;
using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using UI.Localization;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedTextTMP : MonoBehaviour
{
    [Header("Manual Translations")]
    [TextArea(3, 5)] [SerializeField] private string messageEn;
    [TextArea(3, 5)] [SerializeField] private string messageZh;
    [TextArea(1, 2)] [SerializeField] private string messageDefault;

    private TMP_Text _textElement;
    private EventBinding<LanguageChanged> _languageBinding;

    private void Awake()
    {
        _textElement = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        _languageBinding = new EventBinding<LanguageChanged>(OnLanguageChanged);
        EventBus<LanguageChanged>.Register(_languageBinding);
        Refresh();
    }

    private void OnDisable()
    {
        EventBus<LanguageChanged>.Deregister(_languageBinding);
    }

    private void OnLanguageChanged(LanguageChanged evt) => Refresh();

    public void Refresh()
    {
        _textElement.text = ResolveMessage();
    }

    private string ResolveMessage()
    {
        if (LocalizationService.Instance == null) return messageDefault;

        var lang = LocalizationService.Instance.CurrentAppLanguage;

        string result = lang switch
        {
            Language.en => messageEn,
            Language.zh => messageZh,
            _ => messageDefault
        };

        // 若對應語言為空，執行 Fallback 邏輯
        if (string.IsNullOrEmpty(result))
        {
            if (!string.IsNullOrEmpty(messageDefault)) return messageDefault;
            if (!string.IsNullOrEmpty(messageZh)) return messageZh;
            if (!string.IsNullOrEmpty(messageEn)) return messageEn;
        }

        return result ?? "";
    }
}