using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using Cinemachine;

/// <summary>
/// Timeline 過場總管：統一處理輸入鎖定、鏡頭切換、可跳過、淡入淡出。
/// 放在啟動場景（DontDestroyOnLoad），在其他物件/觸發器上呼叫 Play()。
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [Header("Fade (可選)")]
    [Tooltip("全螢幕黑幕的 CanvasGroup（可為空，不使用淡入淡出）")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;

    public enum FadeMode
    {
        /// <summary>不做任何淡入淡出。</summary>
        None = 0,
        /// <summary>開始時確保透明（看得到畫面），結束時不另做處理。</summary>
        StartTransparent,
        /// <summary>進場先黑一下（快速淡黑→淡亮）。</summary>
        DipToBlackAtStart,
        /// <summary>開頭透明，結束時漸黑（常用於接關/切場）。</summary>
        FadeToBlackAtEnd
    }

    [SerializeField] private FadeMode fadeMode = FadeMode.StartTransparent;

    [Header("Skip/控制")]
    [SerializeField] private Key skipKey = Key.Space;

    /// <summary>true=開，false=關</summary>
    public Action<bool> OnTogglePlayerInput;
    /// <summary>true=開，false=關</summary>
    public Action<bool> OnTogglePlayerMovement;
    /// <summary>清除速度（例如剛切過場時避免滑動）</summary>
    public Action OnStopPlayerVelocity;

    private bool _isPlaying;
    private PlayableDirector _current;

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化黑幕狀態
        if (fadeCanvas)
        {
            switch (fadeMode)
            {
                case FadeMode.None:
                case FadeMode.StartTransparent:
                case FadeMode.FadeToBlackAtEnd:
                    SetFade(0f);
                    break;
                case FadeMode.DipToBlackAtStart:
                    SetFade(0f); // 先保持透明，真正播放前才做 Dip
                    break;
            }
        }
    }

    /// <summary>
    /// 播放 Timeline 過場。
    /// </summary>
    /// <param name="director">PlayableDirector（已指定 Timeline Asset）</param>
    /// <param name="optionalVCam">可選的 Cinemachine VCam（播放中提高 Priority）</param>
    /// <param name="onBegin">播放前回調（已鎖操作）</param>
    /// <param name="onComplete">播放後回調（已解鎖）</param>
    /// <param name="allowSkip">是否允許按下 skipKey 跳過</param>
    public void Play(PlayableDirector director,
                     CinemachineVirtualCamera optionalVCam = null,
                     Action onBegin = null,
                     Action onComplete = null,
                     bool allowSkip = true)
    {
        if (_isPlaying || !director) return;
        StartCoroutine(CoPlay(director, optionalVCam, onBegin, onComplete, allowSkip));
    }

    private IEnumerator CoPlay(PlayableDirector director,
                               CinemachineVirtualCamera vcam,
                               Action onBegin,
                               Action onComplete,
                               bool allowSkip)
    {
        _isPlaying = true;
        _current = director;

        // 鎖玩家
        OnTogglePlayerInput?.Invoke(false);
        OnTogglePlayerMovement?.Invoke(false);
        OnStopPlayerVelocity?.Invoke();

        // 提升虛擬鏡頭優先級
        int? savedPriority = null;
        if (vcam)
        {
            savedPriority = vcam.Priority;
            vcam.Priority = 1000;
        }

        // 開始前淡入/淡出策略
        switch (fadeMode)
        {
            case FadeMode.None:
            case FadeMode.StartTransparent:
                yield return EnsureTransparent();
                break;

            case FadeMode.DipToBlackAtStart:
                // 透明 → 快速黑 → 快速亮
                yield return FadeTo(1f);
                yield return null; // 1 frame 停留（可視化更穩定）
                yield return FadeTo(0f);
                break;

            case FadeMode.FadeToBlackAtEnd:
                // 開場保持透明，結尾才黑
                yield return EnsureTransparent();
                break;
        }

        onBegin?.Invoke();

        // 播放 Timeline
        bool finished = false;
        director.time = 0;
        director.extrapolationMode = DirectorWrapMode.None;
        void OnStopped(PlayableDirector d) { finished = true; }
        director.stopped += OnStopped;
        director.Play();

        // 等待結束或跳過
        while (!finished)
        {
            if (allowSkip && Keyboard.current != null && Keyboard.current[skipKey].wasPressedThisFrame)
            {
                director.time = director.duration;
                director.Evaluate();
                director.Stop(); // 觸發 OnStopped
                break;
            }
            yield return null;
        }
        director.stopped -= OnStopped;

        // 收尾淡入/淡出策略
        switch (fadeMode)
        {
            case FadeMode.None:
            case FadeMode.StartTransparent:
            case FadeMode.DipToBlackAtStart:
                // 結尾保持透明
                yield return EnsureTransparent();
                break;

            case FadeMode.FadeToBlackAtEnd:
                // 透明 → 黑
                yield return FadeTo(1f);
                break;
        }

        // 還原鏡頭 & 解鎖
        if (vcam && savedPriority.HasValue) vcam.Priority = savedPriority.Value;
        OnTogglePlayerInput?.Invoke(true);
        OnTogglePlayerMovement?.Invoke(true);

        onComplete?.Invoke();
        _current = null;
        _isPlaying = false;
    }

    // ---------- Fade Helpers ----------

    private IEnumerator EnsureTransparent()
    {
        if (!fadeCanvas) yield break;
        if (fadeCanvas.alpha <= 0.001f)
        {
            SetFade(0f);
            yield break;
        }
        yield return FadeTo(0f);
    }

    private IEnumerator FadeTo(float target)
    {
        if (!fadeCanvas) yield break;
        float start = fadeCanvas.alpha;
        if (Mathf.Approximately(start, target)) yield break;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, fadeDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            SetFade(Mathf.Lerp(start, target, t / dur));
            yield return null;
        }
        SetFade(target);
    }

    private void SetFade(float a)
    {
        if (!fadeCanvas) return;
        fadeCanvas.alpha = a;
        bool block = a > 0.001f;
        fadeCanvas.blocksRaycasts = block;
        fadeCanvas.interactable = block;
    }
}
