using System;
using System.Collections.Generic;
using DefaultNamespace.EventBus;
using DefaultNamespace.EventBus.Events.UI;
using UI.Localization;
using UnityEngine;

public sealed class LocalizationService : MonoBehaviour
{
    public static LocalizationService Instance { get; private set; }

    [Header("Tables")]
    [SerializeField] private LocalizationTable[] tables;

    [Header("Language")]
    [SerializeField] private Language startLanguage = Language.en;
    private const string LANG_PREF_KEY = "APP_LANG";

    public Language CurrentAppLanguage { get; private set; }

    // 給 UI/Objective 使用的系統語言
    public SystemLanguage CurrentSystemLanguage { get; private set; }

    // 給 Ink 使用的代碼（你 Ink 變數 lang 期待 "zh"/"en"/"jp"）
    public string CurrentInkLangCode => ToInkCode(CurrentAppLanguage);

    private Dictionary<SystemLanguage, Dictionary<string, string>> db;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        BuildDb();
        LoadLanguage();               // 讀 PlayerPrefs
        Apply(CurrentAppLanguage, false); // 套用，但不發事件（避免 Awake 期亂觸發）
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildDb()
    {
        db = new Dictionary<SystemLanguage, Dictionary<string, string>>();

        if (tables == null) return;

        foreach (var table in tables)
        {
            if (!table) continue;

            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var e in table.entries)
            {
                if (string.IsNullOrEmpty(e.key)) continue;
                map[e.key] = e.value ?? "";
            }

            db[table.language] = map;
        }
    }

    // === 對外唯一入口：切語言 ===
    public void SetLanguage(Language lang)
    {
        if (CurrentAppLanguage == lang) return;
        Apply(lang, true);
    }

    // 若你還是想用 SystemLanguage 控制（例如從系統語言直接推）
    public void SetLanguage(SystemLanguage sysLang)
    {
        SetLanguage(FromSystemLanguage(sysLang));
    }

    private void Apply(Language lang, bool raiseEvent)
    {
        CurrentAppLanguage = lang;
        CurrentSystemLanguage = ToSystemLanguage(lang);

        PlayerPrefs.SetString(LANG_PREF_KEY, lang.ToString());

        if (raiseEvent)
        {
            // 給 Objective/UI 的語言切換事件
            EventBus<LanguageChanged>.Raise(new LanguageChanged(CurrentSystemLanguage));
        }
    }

    private void LoadLanguage()
    {
        if (!PlayerPrefs.HasKey(LANG_PREF_KEY))
        {
            CurrentAppLanguage = startLanguage;
            PlayerPrefs.SetString(LANG_PREF_KEY, CurrentAppLanguage.ToString());
            return;
        }

        var s = PlayerPrefs.GetString(LANG_PREF_KEY, startLanguage.ToString());
        if (Enum.TryParse(s, out Language lang))
            CurrentAppLanguage = lang;
        else
            CurrentAppLanguage = startLanguage;
    }

    // === 給 Objective/UI 的取字串 ===
    public string Get(string key, object[] args = null)
    {
        if (string.IsNullOrEmpty(key)) return "";

        if (db == null || !db.TryGetValue(CurrentSystemLanguage, out var map))
            return key;

        if (!map.TryGetValue(key, out var text))
            return key;

        if (args == null || args.Length == 0) return text;

        try { return string.Format(text, args); }
        catch { return text; }
    }

    // === mapping ===
    private static SystemLanguage ToSystemLanguage(Language lang)
    {
        return lang switch
        {
            Language.zh => SystemLanguage.ChineseTraditional,
            Language.en   => SystemLanguage.English,
            Language.jp   => SystemLanguage.Japanese,
            _ => SystemLanguage.English
        };
    }

    private static Language FromSystemLanguage(SystemLanguage lang)
    {
        return lang switch
        {
            SystemLanguage.ChineseTraditional => Language.zh,
            SystemLanguage.Chinese => Language.zh, // 若你要簡化
            SystemLanguage.English => Language.en,
            SystemLanguage.Japanese => Language.jp,
            _ => Language.en
        };
    }

    private static string ToInkCode(Language lang)
    {
        return lang switch
        {
            Language.zh => "zh",
            Language.en   => "en",
            Language.jp   => "jp",
            _ => "en"
        };
    }
}
