using System;
using DefaultNamespace.ControlSheme;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UI.Localization;

[ExecuteAlways]
public class TutorialHintTrigger : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup hintCanvasGroup;
    [SerializeField] private bool isTriggerEnter = false;
    [SerializeField] private ActionName actionName;
    [SerializeField] private InputIconDisplay iconShow;

    [SerializeField] private TMP_Text hintText;

    [Header("Message (Per Language)")]
    [TextArea] [SerializeField] private string messageZh;
    [TextArea] [SerializeField] private string messageEn;

    [Header("Fallback (optional)")]
    [TextArea] [SerializeField] private string messageDefault;

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.5f;
    [SerializeField] private float holdTime = 3f;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("Conflict")]
    [SerializeField] private bool skipIfPlaying = true; // 正在播時：true=忽略，false=重啟

    [Header("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = Color.magenta;
    [SerializeField] private bool showWire = false;
    [SerializeField] private Vector3 gizmoOffset = Vector3.zero;

    private bool _played;
    private Sequence _seq;

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (isTriggerEnter) return;
        if (_played) return;

        _played = true;
        ShowHint();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Application.isPlaying) return;
        if (!isTriggerEnter) return;
        if (_played) return;
        if (!other.CompareTag("Player")) return;

        _played = true;
        ShowHint();
    }

    private void OnDisable()
    {
        if (_seq != null && _seq.IsActive()) _seq.Kill(false);
        _seq = null;
    }

    private void ShowHint()
    {
        if (hintCanvasGroup == null || hintText == null) return;

        if (_seq != null && _seq.IsActive())
        {
            if (skipIfPlaying) return;
            _seq.Kill(false);
        }

        hintCanvasGroup.alpha = 0f;
        iconShow.SetAction(actionName);
        hintText.text = ResolveMessage();

        _seq = DOTween.Sequence().SetUpdate(UpdateType.Late);
        _seq.Append(hintCanvasGroup.DOFade(1f, fadeInTime));
        _seq.AppendInterval(holdTime);
        _seq.Append(hintCanvasGroup.DOFade(0f, fadeOutTime));
        _seq.OnComplete(() => _seq = null);
        _seq.SetAutoKill(true);
    }

    private string ResolveMessage()
    {
        // 優先用你的 LocalizationService（與 APP_LANG 同步）
        if (LocalizationService.Instance != null)
        {
            var lang = LocalizationService.Instance.CurrentAppLanguage;

            switch (lang)
            {
                case Language.en:
                    if (!string.IsNullOrEmpty(messageEn)) return messageEn;
                    break;

                case Language.zh:
                    if (!string.IsNullOrEmpty(messageZh)) return messageZh;
                    break;

                // 你目前系統還有 jp，但這支腳本只加了英/中；落到 Default
                case Language.jp:
                default:
                    break;
            }
        }

        // fallback：手動填的 default
        if (!string.IsNullOrEmpty(messageDefault)) return messageDefault;

        // 最後 fallback：有什麼用什麼
        if (!string.IsNullOrEmpty(messageZh)) return messageZh;
        if (!string.IsNullOrEmpty(messageEn)) return messageEn;
        return "";
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (!col) return;

        Gizmos.color = gizmoColor;
        var old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider box)
        {
            Vector3 center = box.center + gizmoOffset;
            Vector3 size = box.size;
            if (showWire) Gizmos.DrawWireCube(center, size);
            else Gizmos.DrawCube(center, size);
        }
        else if (col is SphereCollider sphere)
        {
            Vector3 center = sphere.center + gizmoOffset;
            float radius = sphere.radius;
            if (showWire) Gizmos.DrawWireSphere(center, radius);
            else Gizmos.DrawSphere(center, radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            Vector3 center = capsule.center + gizmoOffset;
            Vector3 size = Vector3.one * capsule.radius * 2f;
            size.y = capsule.height;
            if (showWire) Gizmos.DrawWireCube(center, size);
            else Gizmos.DrawCube(center, size);
        }

        Gizmos.matrix = old;
    }
#endif
}
