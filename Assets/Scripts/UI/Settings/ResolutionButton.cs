using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class ResolutionButton : MonoBehaviour
{
    [System.Serializable]
    public struct Res
    {
        public int width;
        public int height;
        public Res(int w, int h) { width = w; height = h; }
        public override string ToString() => $"{width} x {height}";
    }

    [Header("Auto Wire")]
    [SerializeField] private Button targetButton;          // 不填：自動抓本物件 Button
    [SerializeField] private TextMeshProUGUI valueText;    // 右側顯示 1920 x 1080（可不填）

    [Header("Resolution List (cycle order)")]
    [SerializeField] private Res[] preset = new Res[]
    {
        new Res(1280, 720),
        new Res(1600, 900),
        new Res(1920, 1080),
        new Res(2560, 1440),
    };

    [Header("Apply")]
    [SerializeField] private bool keepFullscreen = true;   // true：維持目前 fullscreen 狀態
    [SerializeField] private bool applyOnStart = false;    // true：啟動就套用目前索引

    private int _index;

    private void Awake()
    {
        if (!targetButton) targetButton = GetComponent<Button>();
        if (!valueText) valueText = GetComponentInChildren<TextMeshProUGUI>(true);

        targetButton.onClick.RemoveListener(OnClickCycle); // 防重複綁定
        targetButton.onClick.AddListener(OnClickCycle);

        _index = FindClosestPresetIndex(Screen.width, Screen.height);

        if (applyOnStart) Apply();
        RefreshText();
    }

    private void OnDestroy()
    {
        if (targetButton) targetButton.onClick.RemoveListener(OnClickCycle);
    }

    private void OnClickCycle()
    {
        if (preset == null || preset.Length == 0) return;

        _index = (_index + 1) % preset.Length;
        Apply();
        RefreshText();
    }

    private void Apply()
    {
        var r = preset[_index];
        bool fs = keepFullscreen ? Screen.fullScreen : false;
        Screen.SetResolution(r.width, r.height, fs);
    }

    private void RefreshText()
    {
        if (!valueText || preset == null || preset.Length == 0) return;
        valueText.text = preset[_index].ToString();
    }

    private int FindClosestPresetIndex(int w, int h)
    {
        if (preset == null || preset.Length == 0) return 0;

        int best = 0;
        long bestScore = long.MaxValue;

        for (int i = 0; i < preset.Length; i++)
        {
            long dx = preset[i].width - w;
            long dy = preset[i].height - h;
            long score = dx * dx + dy * dy;
            if (score < bestScore) { bestScore = score; best = i; }
        }
        return best;
    }
}
