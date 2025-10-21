using System.Collections;
using System.Collections.Generic;
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
                EventBus<SetObjective>.Raise(new SetObjective(s.text));
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
            case StepKind.PlaySFX:
            {
                if (s.audioSource)
                {
                    if (s.clipOverride) s.audioSource.PlayOneShot(s.clipOverride, s.volume);
                    else s.audioSource.Play();
                }
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
}
