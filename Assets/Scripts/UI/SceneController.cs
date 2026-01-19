using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if DOTWEEN
using DG.Tweening;
#endif

/// <summary>
/// 控制場景切換 + Loading 面板顯示/隱藏。
/// 建議把 loadingPanel 設為 SceneController 的子物件，SceneController 設為 DontDestroyOnLoad。
/// </summary>
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Loading UI (作為此物件的子物件)")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private CanvasGroup loadingCanvasGroup;

    [Header("顯示時間")]
    [Tooltip("Loading 至少要顯示多久（秒）")]
    [SerializeField] private float minimumLoadingTime = 2f;

    [Header("可選：淡出動畫（需 DOTween）")]
    [SerializeField] private bool useFadeOut = true;
    [SerializeField, Tooltip("淡出秒數")]
    private float fadeOutDuration = 0.35f;

    // 內部旗標：避免重複載入或重入問題
    private bool _isLoading;

    private void Awake()
    {
        // 單例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 初始化 Loading 參考
        EnsureLoadingRefs();
        // 一開始關閉
        HideLoadingImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 場景載入完成後，下一幀關掉 Loading（保證不殘留 alpha）。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 這裡若有 AudioManager 就播 BGM（防呆）
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnSceneLoaded();

            switch (scene.name)
            {
                case "MainMenu":
                    AudioManager.Instance.PlayBGM(BGMType.MainMenu);
                    break;
                case "L1v5":
                    AudioManager.Instance.PlayBGM(BGMType.FirstLevel);
                    break;
                default:
                    // 其他場景可視需要播放對應 BGM
                    break;
            }
        }

        // 下一幀再關閉，避免與場景內 UI 初始化衝突
        StartCoroutine(HideLoadingNextFrame());
    }

    // =========================
    // 對外 API
    // =========================

    /// <summary>
    /// 以同步方式切換（不建議顯示 loading）
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 以非同步 + 簡易 Loading UI 切換
    /// </summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        if (!_isLoading)
            StartCoroutine(LoadSceneWithSimpleLoadingUI(sceneName));
    }

    /// <summary>
    /// 播放一般 UI 點擊音效（可被 Button 事件綁定）
    /// </summary>
    public void SoundOnClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SFXType.Click);
    }
    
    public void ExitGame()
    {
        Application.Quit();
    }
    // =========================
    // 內部流程
    // =========================

    private IEnumerator LoadSceneWithSimpleLoadingUI(string sceneName)
    {
        _isLoading = true;

        EnsureLoadingRefs();
        ShowLoadingImmediate();

        float start = Time.time;

        // 開始非同步載入，但先不切換場景
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 等待到 0.9（Unity 約定：抵達 0.9 表示就緒）
        while (op.progress < 0.9f)
            yield return null;

        // 保證顯示最少時間
        float elapsed = Time.time - start;
        if (elapsed < minimumLoadingTime)
            yield return new WaitForSeconds(minimumLoadingTime - elapsed);

        // 允許切換，OnSceneLoaded 會負責關閉 Loading
        op.allowSceneActivation = true;

        _isLoading = false;
    }

    private IEnumerator HideLoadingNextFrame()
    {
        yield return null; // 等一幀，等場景內 UI/Canvas 初始化
        HideLoadingAnimatedOrImmediate();
    }

    // =========================
    // 顯示/隱藏輔助
    // =========================

    /// <summary>
    /// 確保有 loadingPanel + CanvasGroup。若無則自動補。
    /// </summary>
    private void EnsureLoadingRefs()
    {
        if (!loadingPanel)
        {
            Debug.LogWarning("[SceneController] loadingPanel 未指定，請在 Inspector 指到 SceneController 的子物件。");
            return;
        }

        if (!loadingCanvasGroup)
        {
            if (!loadingPanel.TryGetComponent(out loadingCanvasGroup))
                loadingCanvasGroup = loadingPanel.AddComponent<CanvasGroup>();
        }
    }

    private void SetCanvasGroup(CanvasGroup group, bool isOn)
    {
        if (!group) return;
        group.alpha = isOn ? 1f : 0f;
        group.blocksRaycasts = isOn;
        group.interactable = isOn;
    }

    private void ShowLoadingImmediate()
    {
        if (!loadingPanel || !loadingCanvasGroup) return;

        loadingPanel.SetActive(true);
        SetCanvasGroup(loadingCanvasGroup, true);

        // 若先前有動畫，重置一下
        #if DOTWEEN
        if (useFadeOut) loadingCanvasGroup.DOKill();
        #endif
    }

    private void HideLoadingImmediate()
    {
        if (!loadingPanel || !loadingCanvasGroup) return;

        #if DOTWEEN
        if (useFadeOut) loadingCanvasGroup.DOKill();
        #endif

        SetCanvasGroup(loadingCanvasGroup, false);
        loadingPanel.SetActive(false);
    }

    private void HideLoadingAnimatedOrImmediate()
    {
        if (!loadingPanel || !loadingCanvasGroup)
            return;

        #if DOTWEEN
        if (useFadeOut && fadeOutDuration > 0f)
        {
            loadingCanvasGroup.DOKill();
            loadingCanvasGroup.DOFade(0f, fadeOutDuration)
                .OnStart(() =>
                {
                    // 在某些切換時機，alpha 可能仍是 1；保險起見先打開
                    loadingPanel.SetActive(true);
                    loadingCanvasGroup.blocksRaycasts = false;
                    loadingCanvasGroup.interactable = false;
                })
                .OnComplete(() =>
                {
                    loadingPanel.SetActive(false);
                });
            return;
        }
        #endif

        // 沒有 DOTween 或不使用淡出時
        HideLoadingImmediate();
    }
}
