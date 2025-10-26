using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class TutorialHintTrigger : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup hintCanvasGroup;  
    [SerializeField] private bool isTriggerEnter = false;

    [SerializeField] private TMP_Text hintText;
    [TextArea] [SerializeField] private string message;

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float holdTime = 3f;
    [SerializeField] private float fadeOutTime = 0.5f;

    private bool _played;

    private void Update()
    {
        if (isTriggerEnter)return;
        if (_played) return;

        _played = true;
        ShowHint();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!isTriggerEnter)return;
        if (_played) return;
        if (!other.CompareTag("Player")) return;

        _played = true;
        ShowHint();
    }

    private void ShowHint()
    {
        if (hintCanvasGroup == null || hintText == null) return;

        hintCanvasGroup.alpha = 0f;
        hintText.text = message;

        // 使用 DOTween 播放淡入 → 停留 → 淡出
        Sequence seq = DOTween.Sequence();
        seq.Append(hintCanvasGroup.DOFade(1f, fadeInTime));
        seq.AppendInterval(holdTime);
        seq.Append(hintCanvasGroup.DOFade(0f, fadeOutTime));
    }
}