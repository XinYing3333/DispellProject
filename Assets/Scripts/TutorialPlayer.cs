using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class TutorialPlayer : MonoBehaviour
{
    public static TutorialPlayer Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject tutorialUI;   // 教學 UI 根物件
    [SerializeField] private RawImage videoOutput;    // 顯示影片的 RawImage
    [SerializeField] private Button closeButton;      // 關閉按鈕（延遲出現）

    [Header("Video Settings")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private float closeButtonDelay = 2f; // 延遲秒數
    [SerializeField] private bool autoHideOnFinish = true;

    [Header("Test Video")]
    [SerializeField]private VideoClip videoClip;
    
    private Coroutine _delayCo;
    private bool isPlaying;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject); return;
        }
        Instance = this;
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseTutorial);
            closeButton.gameObject.SetActive(false);
        }

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;

        HideUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            PlayTutorial(videoClip);
        }
    }

    /// <summary>
    /// 播放指定教學影片
    /// </summary>
    public void PlayTutorial(VideoClip clip)
    {
        if (clip == null || videoPlayer == null)
        {
            Debug.LogWarning("❌ 沒有指定影片或 VideoPlayer 未設置");
            return;
        }

        videoPlayer.clip = clip;
        videoPlayer.Play();

        ShowUI();
        isPlaying = true;

        if (_delayCo != null) StopCoroutine(_delayCo);
        _delayCo = StartCoroutine(ShowCloseButtonAfterDelay(closeButtonDelay));
    }

    private IEnumerator ShowCloseButtonAfterDelay(float delay)
    {
        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        yield return new WaitForSecondsRealtime(delay);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);
    }

    public void CloseTutorial()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (_delayCo != null)
            StopCoroutine(_delayCo);

        HideUI();
        isPlaying = false;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isPlaying = false;
        if (autoHideOnFinish)
            HideUI();
    }

    private void ShowUI()
    {
        if (tutorialUI != null)
            tutorialUI.SetActive(true);
    }

    private void HideUI()
    {
        if (tutorialUI != null)
            tutorialUI.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
    }

    public bool IsPlaying => isPlaying;
}
