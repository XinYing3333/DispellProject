using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TimedDisappearPlatform : MonoBehaviour
{
    private enum State { Ready, CountingDown, Gone, Respawning }

    [Header("Timing")]
    [SerializeField] private float stepDelay = 2f;     // 踩上後多久消失
    [SerializeField] private float respawnDelay = 2f;  // 消失後多久回來

    [Header("Fade")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0); // t:0..1 -> alpha

    [Header("Refs")]
    [SerializeField] private Collider trigger;    // IsTrigger = true
    [SerializeField] private Collider solid;      // 平台實體 Collider (可跟 trigger 同一個也行，但建議分開)
    [SerializeField] private Renderer[] renderers;

    [Header("Player Tag")]
    [SerializeField] private string playerTag = "Player";

    private State _state = State.Ready;

    // 材質處理
    private MaterialPropertyBlock _mpb;
    private int _colorId;
    private Color _baseColor;

    void Awake()
    {
        if (!solid) solid = GetComponent<Collider>();
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);

        _mpb = new MaterialPropertyBlock();
        _colorId = Shader.PropertyToID("_BaseColor"); // URP/Lit 常用
        // 若用的是內建標準材質，可能是 "_Color"；下面會自動 fallback
        CacheBaseColor();
        SetVisible(true);
        SetAlpha(1f);
    }

    private void CacheBaseColor()
    {
        // 盡量從第一個 renderer 抓基礎色
        if (renderers.Length == 0) { _baseColor = Color.white; return; }
        var r = renderers[0];
        var mat = r.sharedMaterial;
        if (!mat) { _baseColor = Color.white; return; }

        if (mat.HasProperty("_BaseColor"))
        {
            _colorId = Shader.PropertyToID("_BaseColor");
            _baseColor = mat.GetColor(_colorId);
        }
        else if (mat.HasProperty("_Color"))
        {
            _colorId = Shader.PropertyToID("_Color");
            _baseColor = mat.GetColor(_colorId);
        }
        else
        {
            _baseColor = Color.white;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_state != State.Ready) return;
        if (!other.CompareTag(playerTag)) return;

        StartCoroutine(CoCycle());
    }

    private IEnumerator CoCycle()
    {
        _state = State.CountingDown;

        // 倒數淡出
        float t = 0f;
        while (t < stepDelay)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / stepDelay);          // 0..1
            float a = fadeCurve.Evaluate(u);                 // 1..0（預設）
            SetAlpha(a);
            yield return null;
        }

        // 消失
        SetAlpha(0f);
        SetVisible(false);
        _state = State.Gone;

        // 等待復活
        yield return new WaitForSeconds(respawnDelay);

        // 顯現
        SetVisible(true);
        SetAlpha(1f);

        _state = State.Ready;
    }

    private void SetVisible(bool visible)
    {
        if (solid) solid.enabled = visible;

        // Trigger 通常保持 enabled，避免狀態錯亂；想要消失期間不再觸發，保持 state 判斷即可
        // if (trigger) trigger.enabled = visible;

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i]) renderers[i].enabled = visible;
    }

    private void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        var c = _baseColor;
        c.a = alpha;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, c);
            r.SetPropertyBlock(_mpb);
        }
    }
}
