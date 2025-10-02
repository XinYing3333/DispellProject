using System;
using UnityEngine;

/// <summary>
/// 衝刺前搖的可視化：地面上一條指向線＋箭頭，線寬/透明度可隨時間變化。
/// 用 LineRenderer，不需要外部資源。播放完自動銷毀並觸發 OnFinished。
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ChargeTelegraph : MonoBehaviour
{
    [Header("Timing")]
    public float duration = 0.6f;            // 前搖時間
    public bool destroyOnFinish = true;
    public event Action OnFinished;

    [Header("Visual")]
    public float startWidth = 0.35f;
    public float endWidth = 0.15f;
    public Color color = new Color(1f, 0.35f, 0.2f, 0.95f);
    public float groundOffset = 0.03f;       // 避免與地面Z-fight
    public bool pulseAlpha = true;           // alpha 脈動提示
    public bool shrinkWidth = true;          // 線寬隨時間收束

    [Header("Arrow Head")]
    public float arrowHeadLength = 1.2f;     // 箭頭長度（世界單位）
    public float arrowHeadWidth  = 0.7f;     // 箭頭寬度（世界單位）

    private LineRenderer _lr;
    private Vector3 _start;
    private Vector3 _end;
    private float _t;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = 4;               // 2點線段 + 2點箭頭（用折線畫）
        _lr.useWorldSpace = true;
        _lr.material = new Material(Shader.Find("Sprites/Default"));
        _lr.numCornerVertices = 2;
        _lr.numCapVertices = 2;
        _lr.textureMode = LineTextureMode.Stretch;
    }

    public void Setup(Vector3 start, Vector3 end, float dur, float w0, float w1, Color c)
    {
        _start = start; _end = end;
        duration = dur > 0 ? dur : duration;
        startWidth = w0 > 0 ? w0 : startWidth;
        endWidth   = w1 > 0 ? w1 : endWidth;
        color = c;
        ApplyVisual(0f);
    }

    private void Update()
    {
        _t += Time.deltaTime;
        float pct = Mathf.Clamp01(_t / duration);
        ApplyVisual(pct);

        if (_t >= duration)
        {
            OnFinished?.Invoke();
            if (destroyOnFinish) Destroy(gameObject);
            enabled = false;
        }
    }

    private void ApplyVisual(float pct)
    {
        // 線寬/alpha 動態
        float w = Mathf.Lerp(startWidth, endWidth, shrinkWidth ? pct : 0f);
        _lr.startWidth = _lr.endWidth = w;

        Color c = color;
        if (pulseAlpha)
        {
            // 0.5~1.0 之間脈動
            float a = Mathf.Lerp(0.5f, 1f, 0.5f + 0.5f * Mathf.Sin(_t * 12f));
            c.a *= a;
        }
        _lr.startColor = _lr.endColor = c;

        // 設定線段與箭頭
        Vector3 s = _start + Vector3.up * groundOffset;
        Vector3 e = _end   + Vector3.up * groundOffset;

        // 主線兩點
        _lr.SetPosition(0, s);
        _lr.SetPosition(1, e);

        // 箭頭兩點（在尾端畫一個V）
        Vector3 dir = (e - s); dir.y = 0f;
        float len = dir.magnitude;
        if (len > 0.001f)
        {
            dir /= len;
            Vector3 right = Quaternion.Euler(0f, 90f, 0f) * dir;
            Vector3 a = e - dir * arrowHeadLength + right * (arrowHeadWidth * 0.5f);
            Vector3 b = e - dir * arrowHeadLength - right * (arrowHeadWidth * 0.5f);
            _lr.SetPosition(2, a);
            _lr.SetPosition(3, b);
        }
        else
        {
            _lr.SetPosition(2, e);
            _lr.SetPosition(3, e);
        }
    }

    /// <summary> 便利的靜態生成。 </summary>
    public static ChargeTelegraph Spawn(Vector3 start, Vector3 end, ChargeTelegraph prefab,
                                        float dur, float w0, float w1, Color color)
    {
        var go = Instantiate(prefab, start, Quaternion.identity);
        var ct = go.GetComponent<ChargeTelegraph>();
        ct.Setup(start, end, dur, w0, w1, color);
        return ct;
    }
}
