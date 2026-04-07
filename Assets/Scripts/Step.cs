// ====================== Step 定義 ======================

using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Video;

[System.Serializable]
public class Step
{
    public StepKind kind;

    [Header("Common")]
    [Tooltip("給 LockMovement/ToggleObject/SetFlag 等需要 bool 的步驟")]
    public bool boolValue;

    [Tooltip("Wait 秒數")]
    public float seconds = 1f;

    // ===== 舊欄位：先保留，避免既有 Step 資料遺失/報錯 =====
    [Tooltip("（舊）任務文字 / 提示文字。遷移後不再使用。")]
    [TextArea] public string text;

    // ===== 新欄位：Objective 用 key/args =====
    [Header("Objective (Localization)")]
    [Tooltip("任務語意鍵，例如：obj_find_exit")]
    public string objectiveKey;

    [Tooltip("格式化參數（對應 {0} {1}...），不需要就留空")]
    public string[] objectiveArgs;

    [Header("Dialogue")]
    public TextAsset inkJSON; // 對話腳本
    public Animator emoteAnimator; // 可選（表情/動作動畫）
    public bool autoPlay = false; 
    public bool lockMoveDuringDialogue = true;

    [Header("Cutscene")]
    public PlayableDirector director; // 直接拖場景裡的 Director
    public CinemachineVirtualCamera vcam; // 可選
    public bool skippable = true;

    [Header("Toggle / Flag")]
    public GameObject targetGO; // 要開關的物件
    public string flagKey; // PlayerPrefs flag_xxx

    [Header("SFX")]
    public SFXType sfx; // 要播放的 AudioSource

    [Header("BGM")]
    public BGMType bgm; // 要播放的 AudioSource

    [Header("Tutorial")]
    public TutorialData data;
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
    PlaySfx,
    PlayBgm,
    PlayTutorial
}