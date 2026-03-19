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
    [Tooltip("（可選）持久化ID。填了就會用 PlayerPrefs 記錄 seq_played_{persistId}=1")]
    public string persistId = ""; // 例：L1_Intro_01

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
        if (onlyOnce && (_playedThisSession || HasPlayedPersistent())) return;

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
        if (onlyOnce) MarkPlayedPersistent();
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
                if (dm == null) yield break;

                var input = PlayerInputHandler.Instance;
                if (s.lockMoveDuringDialogue && input) input.SetLockMovement(true);

                dm.EnterDialogueMode(s.inkJSON, s.emoteAnimator, false, s.lockMoveDuringDialogue);

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
                TutorialPlayer.Instance.PlayTutorial(s.clip);
                yield break;
            }
        }
    }

    private bool HasPlayedPersistent()
    {
        if (string.IsNullOrEmpty(persistId)) return false;
        return PlayerPrefs.GetInt($"seq_played_{persistId}", 0) == 1;
    }

    private void MarkPlayedPersistent()
    {
        if (string.IsNullOrEmpty(persistId)) return;
        PlayerPrefs.SetInt($"seq_played_{persistId}", 1);
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
