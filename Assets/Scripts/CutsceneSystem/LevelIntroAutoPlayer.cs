using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Cinemachine;
using System.Collections;

public class LevelIntroAutoPlayer : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector director;
    public CinemachineVirtualCamera vcam;  // optional

    [Header("Behavior")]
    [Tooltip("空白=用場景名當ID：intro_{SceneName}")]
    public string cutsceneIdOverride = "";
    public bool onlyOnce = true;
    public bool allowSkip = true;
    public int delayFrames = 1; // 等幀數，讓場景穩定後再播
    [SerializeField]private CutsceneManager.FadeMode fadeMode = CutsceneManager.FadeMode.None;

#if UNITY_EDITOR
    [Header("Editor")]
    public bool forceReplayInEditor = false;
#endif

    private void Start()
    {
        if (!director || CutsceneManager.Instance == null) return;

        string id = string.IsNullOrEmpty(cutsceneIdOverride)
            ? $"intro_{SceneManager.GetActiveScene().name}"
            : cutsceneIdOverride;

#if UNITY_EDITOR
        if (forceReplayInEditor) PlayerPrefs.DeleteKey($"cs_played_{id}");
#endif
        if (onlyOnce && PlayerPrefs.GetInt($"cs_played_{id}", 0) == 1)
            return;

        StartCoroutine(CoPlay(id));
    }

    private IEnumerator CoPlay(string id)
    {
        // 等幾幀，確保玩家/相機/音頻都初始化完成
        for (int i = 0; i < Mathf.Max(0, delayFrames); i++) yield return null;

        // 防止 Director 自己 PlayOnAwake 造成雙播
        if (director.state == PlayState.Playing) yield break;
        
        Time.timeScale = 0;
        CutsceneManager.Instance.SetFadeMode(fadeMode);
        CutsceneManager.Instance.Play(
            director,
            vcam,
            onBegin: null,
            onComplete: () =>
            {
                if (onlyOnce) PlayerPrefs.SetInt($"cs_played_{id}", 1);
                Time.timeScale = 1;
            },
            allowSkip: allowSkip
        );
    }
}