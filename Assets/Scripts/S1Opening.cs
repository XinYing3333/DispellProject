using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;
using Cinemachine;
using Player;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 進觸發 → 先播片頭 VideoClip → 播完或被跳過後再交給 CutsceneManager 播 Timeline。
/// 支援：play-once、skip、PlayerPrefs 持久化、可選 vcam、按住 B 2 秒跳過片頭 + 視覺進度。
/// </summary>
[RequireComponent(typeof(Collider))]
public class S1Opening : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("真正的 Timeline")]
    public PlayableDirector director;

    [Tooltip("可選：cutscene 時要切到的虛擬攝影機")]
    public CinemachineVirtualCamera vcam;

    [Header("Intro Video (optional)")]
    [Tooltip("如果有指定，就會先播這個影片，播完才開始 Timeline")]
    public VideoPlayer introPlayer;

    [Tooltip("片頭播完後，是否自動隱藏它的物件")]
    public bool hideIntroAfterPlay = true;

    [Header("ID / Persistence")]
    [Tooltip("唯一ID，用來做只播一次的 PlayerPrefs key")]
    public string cutsceneId = "Cutscene_Default_ID";

    [Header("Trigger")]
    public bool playOnEnter = true;
    public bool playOnStart = false;
    public bool onlyOnce = true;
    public bool allowSkip = true;

    [Header("Auto Binding (optional)")]
    public bool autoBindExposedReferences = true;

    [Header("Skip Hold")]
    [Tooltip("按住多久才跳過（秒）")]
    [SerializeField] private float holdToSkipSeconds = 2f;

    [Tooltip("顯示跳過提示的 UI Root（可選）")]
    [SerializeField] private GameObject skipUIRoot;

    [Tooltip("用來顯示進度的 Image（Type 設為 Filled）")]
    [SerializeField] private Image skipFillImage;

    [Header("Events")]
    [Tooltip("Cutscene 播放結束後執行的事件（可在 Inspector 指派）")]
    public UnityEvent onCutsceneFinished;

    // 可在程式碼中註冊
    public UnityAction onCutsceneFinishedAction;

    private bool _playedThisSession;
    private bool _isPlayingIntro;
    private bool _isPlayingCutscene;

    // hold state
    private float _holdTimer;
    private bool _holdActive;

    private void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (introPlayer != null)
            introPlayer.gameObject.SetActive(false);

        SetSkipUI(false, 0f);

        if (playOnStart)
            TryPlay();
    }

    private void Update()
    {
        if (!_isPlayingIntro) return;
        if (!allowSkip) { SetSkipUI(false, 0f); return; }

        // 只在 Intro 播放期間顯示/更新 UI
        SetSkipUI(true, GetHold01());

        // 按住 B 進行累計；鬆開就歸零
        bool holding = Input.GetKey(KeyCode.B);
        bool holding2 = PlayerInputHandler.Instance.IsSkiping;

        // 如果你仍想兼容舊的「互動鍵」：按下時啟動一次 hold，但無法判定持續按住時會停在那裡（取決於你的輸入系統）
        // 這段不會造成一按就跳過；只會嘗試啟動 hold。
        if (!_holdActive && PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.InteractPressed)
            _holdActive = true;

        if (holding || holding2)
        {
            _holdActive = true;
            _holdTimer += Time.unscaledDeltaTime;

            if (_holdTimer >= holdToSkipSeconds)
            {
                _holdTimer = 0f;
                _holdActive = false;
                SetSkipUI(false, 0f);
                SkipIntroAndStartCutscene();
            }
        }
        else
        {
            // 沒有持續按住 B 就重置
            _holdTimer = 0f;
            _holdActive = false;
        }

        // 每幀更新 fill
        if (skipFillImage != null)
            skipFillImage.fillAmount = GetHold01();
    }

    private float GetHold01()
    {
        if (holdToSkipSeconds <= 0f) return 1f;
        return Mathf.Clamp01(_holdTimer / holdToSkipSeconds);
    }

    private void SetSkipUI(bool on, float fill01)
    {
        if (skipUIRoot != null && skipUIRoot.activeSelf != on)
            skipUIRoot.SetActive(on);

        if (skipFillImage != null)
            skipFillImage.fillAmount = on ? Mathf.Clamp01(fill01) : 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playOnEnter) return;
        if (!other.CompareTag("Player")) return;
        TryPlay();
    }

    /// <summary>
    /// 外部也能叫。會自動處理「有沒有播過」與「有沒有片頭」。
    /// </summary>
    public void TryPlay()
    {
        if (director == null || CutsceneManager.Instance == null)
            return;

        if (onlyOnce)
        {
            if (_playedThisSession) return;
            if (HasPlayedPersistent()) return;
        }

        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.SetLockMovement(true);

        if (introPlayer != null)
        {
            introPlayer.gameObject.SetActive(true);
            PlayIntro();
        }
        else
        {
            PlayCutscene();
        }
    }

    private void PlayIntro()
    {
        if (introPlayer == null) return;

        _isPlayingIntro = true;
        _holdTimer = 0f;
        _holdActive = false;
        SetSkipUI(allowSkip, 0f);

        introPlayer.loopPointReached -= OnIntroFinished;
        introPlayer.errorReceived    -= OnIntroError;

        introPlayer.loopPointReached += OnIntroFinished;
        introPlayer.errorReceived    += OnIntroError;

        introPlayer.Play();
    }

    private void OnIntroFinished(VideoPlayer vp)
    {
        _isPlayingIntro = false;
        _holdTimer = 0f;
        _holdActive = false;
        SetSkipUI(false, 0f);

        if (hideIntroAfterPlay)
            vp.gameObject.SetActive(false);

        PlayCutscene();
    }

    private void OnIntroError(VideoPlayer vp, string msg)
    {
        Debug.LogWarning($"Intro video error: {msg}, will still play cutscene.");
        _isPlayingIntro = false;
        _holdTimer = 0f;
        _holdActive = false;
        SetSkipUI(false, 0f);
        PlayCutscene();
    }

    private void SkipIntroAndStartCutscene()
    {
        if (introPlayer != null)
        {
            introPlayer.Stop();
            if (hideIntroAfterPlay)
                introPlayer.gameObject.SetActive(false);
        }

        _isPlayingIntro = false;
        _holdTimer = 0f;
        _holdActive = false;
        SetSkipUI(false, 0f);

        PlayCutscene();
    }

    private void PlayCutscene()
    {
        if (director == null || CutsceneManager.Instance == null) return;

        _isPlayingCutscene = true;

        CutsceneManager.Instance.Play(
            director,
            vcam,
            onBegin: null,
            onComplete: OnCutsceneComplete,
            allowSkip: allowSkip
        );
    }

    private void OnCutsceneComplete()
    {
        _isPlayingCutscene = false;
        _playedThisSession = true;
        if (onlyOnce) MarkPlayedPersistent();

        onCutsceneFinished?.Invoke();
        onCutsceneFinishedAction?.Invoke();
    }

    public void Skip()
    {
        if (!allowSkip) return;

        if (_isPlayingIntro && introPlayer != null)
        {
            introPlayer.Stop();
            if (hideIntroAfterPlay)
                introPlayer.gameObject.SetActive(false);
            _isPlayingIntro = false;
        }

        if (_isPlayingCutscene && director != null)
        {
            director.Stop();
            _isPlayingCutscene = false;
        }

        _holdTimer = 0f;
        _holdActive = false;
        SetSkipUI(false, 0f);

        _playedThisSession = true;
        if (onlyOnce) MarkPlayedPersistent();

        onCutsceneFinished?.Invoke();
        onCutsceneFinishedAction?.Invoke();
    }

    // ======= PlayerPrefs persistence =======
    private bool HasPlayedPersistent()
        => PlayerPrefs.GetInt($"cs_played_{cutsceneId}", 0) == 1;

    private void MarkPlayedPersistent()
        => PlayerPrefs.SetInt($"cs_played_{cutsceneId}", 1);
}
