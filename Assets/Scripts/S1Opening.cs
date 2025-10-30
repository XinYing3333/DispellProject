using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;
using Cinemachine;
using Player;
using UnityEngine.Events;

/// <summary>
/// 進觸發 → 先播片頭 VideoClip → 播完或被跳過後再交給 CutsceneManager 播 Timeline。
/// 支援：play-once、skip、PlayerPrefs 持久化、可選 vcam、按 Y 跳過片頭。
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

    [Header("Events")]
    [Tooltip("Cutscene 播放結束後執行的事件（可在 Inspector 指派）")]
    public UnityEvent onCutsceneFinished;
    
    // 可在程式碼中註冊
    public UnityAction onCutsceneFinishedAction;

    private bool _playedThisSession;
    private bool _isPlayingIntro;
    private bool _isPlayingCutscene;

    private void Start()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        introPlayer.gameObject.SetActive(false);

        if (playOnStart)
        {
            TryPlay();
        }
    }

    private void Update()
    {
        // 只有在「影片正在播」的時候才聽按鍵
        if (_isPlayingIntro)
        {
            if (Input.GetKeyDown(KeyCode.B) || PlayerInputHandler.Instance.InteractPressed)
            {
                SkipIntroAndStartCutscene();
            }
        }
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

        introPlayer.gameObject.SetActive(true);
        if (introPlayer != null)
        {
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

        introPlayer.loopPointReached -= OnIntroFinished;
        introPlayer.errorReceived    -= OnIntroError;

        introPlayer.loopPointReached += OnIntroFinished;
        introPlayer.errorReceived    += OnIntroError;

        introPlayer.Play();
    }

    private void OnIntroFinished(VideoPlayer vp)
    {
        _isPlayingIntro = false;
        if (hideIntroAfterPlay)
            vp.gameObject.SetActive(false);

        PlayCutscene();
    }

    private void OnIntroError(VideoPlayer vp, string msg)
    {
        Debug.LogWarning($"Intro video error: {msg}, will still play cutscene.");
        _isPlayingIntro = false;
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

        // 執行所有 callback
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

        _playedThisSession = true;
        if (onlyOnce) MarkPlayedPersistent();

        // 同樣觸發結束事件（如果 skip 時也想執行）
        onCutsceneFinished?.Invoke();
        onCutsceneFinishedAction?.Invoke();
    }

    // ======= PlayerPrefs persistence =======
    private bool HasPlayedPersistent()
        => PlayerPrefs.GetInt($"cs_played_{cutsceneId}", 0) == 1;

    private void MarkPlayedPersistent()
        => PlayerPrefs.SetInt($"cs_played_{cutsceneId}", 1);
}
