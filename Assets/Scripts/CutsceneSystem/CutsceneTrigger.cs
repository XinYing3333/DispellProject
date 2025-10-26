using System;
using UnityEngine;
using UnityEngine.Playables;
using Cinemachine;

/// <summary>
/// Trigger a Timeline cutscene when player enters a trigger volume.
/// Supports: play-once, skip, optional auto-binding, and a simple persistent flag via PlayerPrefs.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CutsceneTrigger : MonoBehaviour
{
    [Header("Setup")]
    public PlayableDirector director;
    public CinemachineVirtualCamera vcam; // optional
    [Tooltip("Unique ID for persistence. Use a stable string, e.g., 'Cutscene_L1_Intro'.")]
    public string cutsceneId = "Cutscene_Default_ID";

    [Header("Trigger")]
    public bool playOnEnter = true;
    public bool playOnStart = false;
    public bool onlyOnce = true;
    public bool allowSkip = true;

    [Header("Auto Binding (optional)")]
    public bool autoBindExposedReferences = true;

    private bool _playedThisSession;


    private void Start()
    {
        if (playOnStart)
        {
            TryPlay();
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playOnEnter) return;
        if (!other.CompareTag("Player")) return;
        TryPlay();
    }

    /// <summary>
    /// Try to play the cutscene immediately.
    /// You can call this from script/UI/button as well.
    /// </summary>
    public void TryPlay()
    {
        if (director == null || CutsceneManager.Instance == null) return;

        if (onlyOnce)
        {
            if (_playedThisSession) return;
            if (HasPlayedPersistent()) return;
        }

        // if (autoBindExposedReferences)
        //     TimelineAutoBinder.Bind(director.gameObject);

        CutsceneManager.Instance.Play(
            director,
            vcam,
            onBegin: null,
            onComplete: () =>
            {
                _playedThisSession = true;
                if (onlyOnce) MarkPlayedPersistent();
            },
            allowSkip: allowSkip
        );
    }

    // ======= Simple persistence via PlayerPrefs (replace with your save system if needed) =======
    private bool HasPlayedPersistent() => PlayerPrefs.GetInt($"cs_played_{cutsceneId}", 0) == 1;
    private void MarkPlayedPersistent() => PlayerPrefs.SetInt($"cs_played_{cutsceneId}", 1);
}
