using System.Collections;
using System.Collections.Generic;
using DefaultNamespace.EventBus.Events.Core;
using UnityEngine;
using DefaultNamespace.EventBus.Events.UI;
using Player;

[RequireComponent(typeof(Collider))]
public class LevelSequenceTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("玩家進入觸發器就執行")]
    public bool playOnEnter = true;
    public string requiredTag = "Player";
    [Tooltip("冷卻（避免連撞重複觸發）")]
    public float cooldown = 0.5f;

    [Header("One-shot / Persistence")]
    [Tooltip("是否只執行一次（本場景生命週期）")]
    public bool onlyOnce = true;
    
    [Tooltip("（自動產生）持久化ID。用於記錄此觸發器是否已執行過。")]
    [SerializeField] private string persistId = ""; 
    public string PersistId => persistId;

    [Header("Steps (依序執行)")]
    public List<Step> steps = new();

    // ---------------- Gizmo 設定 ----------------
    [Header("Gizmo Settings")]
    [Tooltip("是否顯示觸發區範圍 (SceneView)")]
    public bool showGizmo = true;
    public Color onceGizmoColor = new Color(1f, 0.6f, 0.2f, 0.25f);
    public Color onceGizmoWireColor = new Color(1f, 0.6f, 0.2f, 0.9f);
    public Color gizmoColor = new Color(1f, 0.6f, 0.2f, 0.25f);
    public Color gizmoWireColor = new Color(1f, 0.6f, 0.2f, 0.9f);
    
    // --- runtime ---
    private float _lastTime = -999f;
    private bool _playedThisSession;
    private Coroutine _co;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // 第一次掛載時自動生成
        if (string.IsNullOrEmpty(persistId))
        {
            GenerateId();
        }
    }
    
    private void OnValidate()
    {
        // 確保在 Inspector 編輯時，如果開啟了 onlyOnce 卻沒 ID，就補上
        if (onlyOnce && string.IsNullOrEmpty(persistId))
        {
            GenerateId();
        }
    }
    
    /// <summary>
    /// 右鍵點擊組件名稱可手動重新生成
    /// </summary>
    [ContextMenu("Generate New Persist ID")]
    public void GenerateId()
    {
        // 格式：場景名_物件名_唯一碼 (增加辨識度)
        string sceneName = gameObject.scene.name ?? "Prefab";
        string uniquePart = System.Guid.NewGuid().ToString().Substring(0, 8);
        persistId = $"{sceneName}_{gameObject.name}_{uniquePart}";
        
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playOnEnter) return;
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
        Play();
    }

    /// <summary>可由事件/按鈕/程式呼叫</summary>
    public void Play()
    {
        if (Time.time - _lastTime < cooldown) return;
        
        // 修改：使用 DataManager 檢查是否已觸發過
        if (onlyOnce && (_playedThisSession || DataManager.Instance.IsTriggered(persistId))) 
            return;

        _lastTime = Time.time;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Co_Run());
    }

    private IEnumerator Co_Run()
    {
        foreach (var s in steps)
        {
            if (s == null) continue;
            yield return ExecuteStep(s);
        }

        _playedThisSession = true;
        
        // 修改：完成後記在 DataManager 的暫存清單中
        if (onlyOnce && !string.IsNullOrEmpty(persistId))
        {
            DataManager.Instance.sessionTriggeredIds.Add(persistId);
        }
    }

    private IEnumerator ExecuteStep(Step s)
    {
        switch (s.kind)
        {
            case StepKind.LockMovement:
            {
                var input = PlayerInputHandler.Instance;
                if (input) input.SetLockMovement(s.boolValue);
                yield break;
            }
            case StepKind.PlayCutscene:
            {
                if (!s.director) yield break;

                bool done = false;
                if (CutsceneManager.Instance != null)
                {
                    CutsceneManager.Instance.Play(
                        s.director,
                        s.vcam,               // 可為 null
                        onBegin: null,
                        onComplete: () => done = true,
                        allowSkip: s.skippable
                    );
                    while (!done) yield return null;
                }
                yield break;
            }
            case StepKind.StartDialogue:
            {
                if (!s.inkJSON) yield break;

                var dm = DialogueManager.GetInstance();
                if (dm == null || dm.dialogueIsPlaying) yield break;

                var input = PlayerInputHandler.Instance;
                if (s.lockMoveDuringDialogue && input) input.SetLockMovement(true);
                dm.EnterDialogueMode(s.inkJSON, s.emoteAnimator, s.autoPlay, s.lockMoveDuringDialogue);

                while (dm.dialogueIsPlaying) yield return null;

                if (s.lockMoveDuringDialogue && input) input.SetLockMovement(false);
                yield break;
            }
            case StepKind.SetObjective:
            {
                var key = !string.IsNullOrEmpty(s.objectiveKey) ? s.objectiveKey : s.text;

                object[] args = null;
                if (s.objectiveArgs != null && s.objectiveArgs.Length > 0)
                {
                    args = new object[s.objectiveArgs.Length];
                    for (int i = 0; i < s.objectiveArgs.Length; i++)
                        args[i] = s.objectiveArgs[i];
                }

                EventBus<SetObjective>.Raise(new SetObjective(key, args));
                yield break;
            }

            case StepKind.Wait:
            {
                float t = 0f;
                while (t < s.seconds)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                yield break;
            }
            case StepKind.ToggleObject:
            {
                if (s.targetGO) s.targetGO.SetActive(s.boolValue);
                yield break;
            }
            case StepKind.SetFlag:
            {
                if (!string.IsNullOrEmpty(s.flagKey))
                    PlayerPrefs.SetInt($"flag_{s.flagKey}", s.boolValue ? 1 : 0);
                yield break;
            }
            case StepKind.PlaySfx:
            {
                AudioManager.Instance.PlaySFX(s.sfx);
                yield break;
            }
            case StepKind.PlayBgm:
            {
                AudioManager.Instance.StopBGM();
                AudioManager.Instance.PlayBGM(s.bgm);
                yield break;
            }
            case StepKind.PlayTutorial:
            {
                TutorialManager.Instance.TriggerTutorial(s.data);
                yield break;
            }
        }
    }
    
    // ---------------- Gizmo 繪製 ----------------
    private void OnDrawGizmos()
    {
        if (!playOnEnter || !showGizmo) return;

        var col = GetComponent<Collider>();
        if (!col) return;

        Gizmos.color = gizmoColor;

        // 根據 collider 類型畫不同形狀
        if (col is BoxCollider box)
        {
            var m = Matrix4x4.TRS(transform.TransformPoint(box.center), transform.rotation, transform.lossyScale);
            using (new GizmosMatrixScope(m))
            {
                if (onlyOnce)
                {
                    Gizmos.color = onceGizmoColor;
                    Gizmos.DrawCube(Vector3.zero, box.size);
                    Gizmos.color = onceGizmoWireColor;
                }
                else
                {
                    Gizmos.color = gizmoColor;
                    Gizmos.DrawCube(Vector3.zero, box.size);
                    Gizmos.color = gizmoWireColor;
                }

                Gizmos.DrawWireCube(Vector3.zero, box.size);
            }
        }
    }
    /// <summary>
    /// 用於在 Gizmos 畫多層次物件時安全恢復矩陣的輔助類。
    /// </summary>
    private readonly struct GizmosMatrixScope : System.IDisposable
    {
        private readonly Matrix4x4 _old;
        public GizmosMatrixScope(Matrix4x4 m)
        {
            _old = Gizmos.matrix;
            Gizmos.matrix = m;
        }
        public void Dispose() => Gizmos.matrix = _old;
    }
}
