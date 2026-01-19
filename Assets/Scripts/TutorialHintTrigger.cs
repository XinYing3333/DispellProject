using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

[ExecuteAlways]
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

    [Header("Conflict")]
    [SerializeField] private bool skipIfPlaying = true; // 正在播時：true=忽略，false=重啟

    [Header("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = Color.magenta;
    [SerializeField] private bool showWire = false;
    [SerializeField] private Vector3 gizmoOffset = Vector3.zero;

    private bool _played;
    private Sequence _seq; // ★ 新增：用來判斷是否正在播放，並可中止

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
        // 避免物件被關閉時還留著 Tween
        if (_seq != null && _seq.IsActive()) _seq.Kill(false);
        _seq = null;
    }

    private void ShowHint()
    {
        if (hintCanvasGroup == null || hintText == null) return;

        // ★ 核心：衝突處理（超精簡）
        if (_seq != null && _seq.IsActive())
        {
            if (skipIfPlaying) return;   // 正在播就略過
            _seq.Kill(false);            // 否則中止舊的，重啟新的
        }

        hintCanvasGroup.alpha = 0f;
        hintText.text = message;

        _seq = DOTween.Sequence().SetUpdate(UpdateType.Late); // 用 Late 減少相對抖動
        _seq.Append(hintCanvasGroup.DOFade(1f, fadeInTime));
        _seq.AppendInterval(holdTime);
        _seq.Append(hintCanvasGroup.DOFade(0f, fadeOutTime));
        _seq.OnComplete(() => _seq = null);
        _seq.SetAutoKill(true);
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
