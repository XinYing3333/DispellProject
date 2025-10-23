using UnityEngine;
using System.Collections.Generic;

public class RoadFader : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("可留空，會自動抓取子物件的 Renderer")]
    public List<Renderer> renderers = new();

    [Header("Dissolve Property")]
    [Tooltip("對應 Shader 中用來控制顯示/溶解的浮點參數名稱")]
    public string propertyName = "_visble_amount"; // ← 依你的 Shader 寫法

    [Header("Visible Range (0~1)")]
    [Range(0f, 1f)] public float hiddenAmount = 1f; // 完全溶掉
    [Range(0f, 1f)] public float shownAmount  = 0.1f; // 完全顯示

    [Header("Step 模式（對齊你原本的寫法）")]
    [Tooltip("每次步進的量（越大越快）")]
    public float dissolveRate = 0.05f;
    [Tooltip("每次更新之間的等待秒數")]
    public float refreshRate = 0.02f;

    // 快取
    private readonly List<MaterialPropertyBlock> _mpbs = new();
    private int _propId;
    private bool _hasAnyRenderer;
    private float _currentAmount = 0f;


    void Awake()
    {
        if (renderers == null || renderers.Count == 0)
            renderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true));

        _hasAnyRenderer = renderers != null && renderers.Count > 0;

        _mpbs.Clear();
        foreach (var r in renderers)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            _mpbs.Add(mpb);
        }

        _propId = Shader.PropertyToID(propertyName);

        _currentAmount = Mathf.Clamp01(hiddenAmount);   // ← 用隱藏初值初始化
        // 一開始先設成隱藏狀態
        SetDissolveImmediate(hiddenAmount);
    }

    #region 供外部呼叫的 API
    public System.Collections.IEnumerator FadeIn(float duration)
    {
        // 時間插值版：從目前值 → shownAmount
        yield return DissolveTo(shownAmount, duration);
    }

    public System.Collections.IEnumerator FadeOut(float duration)
    {
        // 時間插值版：從目前值 → hiddenAmount
        yield return DissolveTo(hiddenAmount, duration);
    }

    public System.Collections.IEnumerator Appear()     // 對齊你原本 coroutine 名稱
        => Appear_StepMode();

    public System.Collections.IEnumerator Disappear()  // 對齊你原本 coroutine 名稱
        => Disappear_StepMode();
    #endregion

    /// <summary>
    /// 立刻把所有 renderer 的 _visble_amount 設為 v（不經過動畫）
    /// </summary>
    public void SetDissolveImmediate(float v)
    {
        if (!_hasAnyRenderer) return;

        v = Mathf.Clamp01(v);
        _currentAmount = v;                             // ← 這裡更新快取

        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i]; if (!r) continue;
            var mpb = _mpbs[i];
            mpb.SetFloat(_propId, v);
            r.SetPropertyBlock(mpb);
        }
    }


    /// <summary>
    /// 時間插值版（建議平滑）：把目前值在 duration 秒內推到 target
    /// </summary>
    private System.Collections.IEnumerator DissolveTo(float target, float duration)
    {
        if (!_hasAnyRenderer) yield break;

        target = Mathf.Clamp01(target);

        float start = _currentAmount;                   // ← 用快取，不去讀材質
        if (duration <= 0f) { SetDissolveImmediate(target); yield break; }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float v = Mathf.Lerp(start, target, t / duration);
            SetDissolveImmediate(v);                   // ← 這裡會同步更新 _currentAmount
            yield return null;
        }
        SetDissolveImmediate(target);
    }

    /// <summary>
    /// 讀目前值（從第一個 Renderer 的 PropertyBlock 取，取不到則 0）
    /// </summary>
    private float GetCurrentAmount()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i]; if (!r) continue;
            var mpb = _mpbs[i];

            // 重新抓一次 PB（避免外部有改動）
            r.GetPropertyBlock(mpb);

            // 無法直接 GetFloat，只能靠材質讀；讀不到就回 0
            // ※ 若材質 Animation 在跑，這裡會讀到材質初值
            var mats = r.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m]; if (!mat) continue;
                if (mat.HasProperty(_propId))
                    return mat.GetFloat(_propId);
            }
        }
        return 1f;
    }

    #region 你的原版步進寫法（等價移植）
    private System.Collections.IEnumerator Appear_StepMode()
    {
        if (!_hasAnyRenderer) yield break;

        // 讀目前值當作起點
        float counter = Mathf.Clamp01(GetCurrentAmount());

        if (counter < 1f)
        {
            while (counter < 1f)
            {
                counter = Mathf.Min(1f, counter + Mathf.Abs(dissolveRate));
                SetDissolveImmediate(counter);
                yield return new WaitForSeconds(Mathf.Max(0f, refreshRate));
            }
        }
    }

    private System.Collections.IEnumerator Disappear_StepMode()
    {
        if (!_hasAnyRenderer) yield break;

        float counter = Mathf.Clamp01(GetCurrentAmount());
        if (counter > 0f)
        {
            while (counter > 0f)
            {
                counter = Mathf.Max(0f, counter - Mathf.Abs(dissolveRate));
                SetDissolveImmediate(counter);
                yield return new WaitForSeconds(Mathf.Max(0f, refreshRate));
            }
        }
    }
    #endregion
}
