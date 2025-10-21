// ====================== Step 定義 ======================

using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class Step
{
    public StepKind kind;

    [Header("Common")]
    [Tooltip("給 LockMovement/ToggleObject/SetFlag 等需要 bool 的步驟")]
    public bool boolValue;
    [Tooltip("Wait 秒數")]
    public float seconds = 1f;
    [Tooltip("任務文字 / 提示文字")]
    [TextArea] public string text;

    [Header("Dialogue")]
    public TextAsset inkJSON;          // 對話腳本
    public Animator emoteAnimator;     // 可選（表情/動作動畫）
    public bool lockMoveDuringDialogue = true;

    [Header("Cutscene")]
    public PlayableDirector director;  // 直接拖場景裡的 Director
    public CinemachineVirtualCamera vcam; // 可選
    public bool skippable = true;

    [Header("Toggle / Flag")]
    public GameObject targetGO;        // 要開關的物件
    public string flagKey;             // PlayerPrefs flag_xxx

    [Header("SFX")]
    public AudioSource audioSource;    // 要播放的 AudioSource
    public AudioClip clipOverride;     // 可選：覆寫 clip
    [Range(0f, 1f)] public float volume = 1f;
}

public enum StepKind
{
    LockMovement,
    PlayCutscene,
    StartDialogue,
    SetObjective,
    Wait,
    ToggleObject,
    SetFlag,
    PlaySFX
}