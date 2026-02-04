using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Localization; // Language enum

// 掛在「Language 按鈕」物件上（同物件有 Button）
// 功能：按一下在 en <-> zh 間切換，並呼叫 LocalizationService.Instance.SetLanguage(...)
public sealed class LanguageButton : MonoBehaviour
{
    [Header("Auto Wire")]
    [SerializeField] private Button targetButton;        // 不填：自動抓本物件 Button
    [SerializeField] private TextMeshProUGUI valueText;  // 右側顯示文字（不填：自動抓子物件 TMP）

    [Header("Cycle Order")]
    [SerializeField] private Language[] cycle = { Language.en, Language.zh }; // 目前只要英/繁

    private int _index;

    private void Awake()
    {
        if (!targetButton) targetButton = GetComponent<Button>();
        if (!valueText) valueText = GetComponentInChildren<TextMeshProUGUI>(true);

        targetButton.onClick.RemoveListener(OnClickCycle);
        targetButton.onClick.AddListener(OnClickCycle);

        // 以 LocalizationService 的目前語言決定 index
        var svc = LocalizationService.Instance;
        if (svc != null)
        {
            _index = FindIndex(svc.CurrentAppLanguage);
            RefreshText(svc.CurrentAppLanguage);
        }
        else
        {
            _index = 0;
            RefreshText(cycle.Length > 0 ? cycle[0] : Language.en);
        }
    }

    private void OnDestroy()
    {
        if (targetButton) targetButton.onClick.RemoveListener(OnClickCycle);
    }

    private void OnClickCycle()
    {
        if (cycle == null || cycle.Length == 0) return;

        _index = (_index + 1) % cycle.Length;
        var next = cycle[_index];

        // ★ 接入你的 LocalizationService 介面
        if (LocalizationService.Instance != null)
            LocalizationService.Instance.SetLanguage(next);

        RefreshText(next);
    }

    private int FindIndex(Language lang)
    {
        if (cycle == null || cycle.Length == 0) return 0;
        for (int i = 0; i < cycle.Length; i++)
            if (cycle[i] == lang) return i;
        return 0;
    }

    private void RefreshText(Language lang)
    {
        if (!valueText) return;

        valueText.text = lang switch
        {
            Language.en => "English",
            Language.zh => "繁體中文",
            _ => lang.ToString()
        };
    }
}
