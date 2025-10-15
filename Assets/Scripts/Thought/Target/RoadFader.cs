using UnityEngine;
using System.Collections.Generic;

public class RoadFader : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("可留空，會自動抓取子物件的 Renderer")]
    public List<Renderer> renderers = new();

    [Header("Alpha 範圍")]
    [Range(0, 1)] public float hiddenAlpha = 0f;
    [Range(0, 1)] public float shownAlpha  = 1f;

    // 快取
    private readonly List<MaterialPropertyBlock> _mpbs = new();
    private readonly List<string> _colorProps = new(); // 每個Renderer對應的顏色屬性名

    void Awake()
    {
        if (renderers == null || renderers.Count == 0)
            renderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true));

        _mpbs.Clear(); _colorProps.Clear();
        foreach (var r in renderers)
        {
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);

            // 嘗試找可用的顏色屬性名（URP 是 _BaseColor；內建常見 _Color）
            string prop = "_BaseColor";
            bool has = false;
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                if (m.HasProperty("_BaseColor")) { prop = "_BaseColor"; has = true; break; }
                if (m.HasProperty("_Color"))     { prop = "_Color";     has = true; break; }
            }
            if (!has) prop = "_Color"; // 盡量給個預設

            _mpbs.Add(mpb);
            _colorProps.Add(prop);
        }

        SetInstant(hiddenAlpha);
    }

    public System.Collections.IEnumerator FadeIn(float duration)
    {
        yield return FadeTo(shownAlpha, duration);
    }

    public System.Collections.IEnumerator FadeOut(float duration)
    {
        yield return FadeTo(hiddenAlpha, duration);
    }

    public void SetInstant(float alpha)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i]; if (!r) continue;
            var mpb = _mpbs[i];
            var prop = _colorProps[i];

            // 讀出顏色 → 改 alpha → 寫回
            Color c = Color.white;
            // 嘗試從任一材質拿當前顏色（取第一個有該屬性的材質）
            var mats = r.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m]; if (!mat) continue;
                if (mat.HasProperty(prop)) { c = mat.GetColor(prop); break; }
            }
            c.a = alpha;

            mpb.SetColor(prop, c);
            r.SetPropertyBlock(mpb);
        }
    }

    private System.Collections.IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetInstant(targetAlpha);
            yield break;
        }

        // 取第一個 renderer 的目前 alpha 當起點
        float startA = GetCurrentAlpha();
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, targetAlpha, t / duration);
            SetInstant(a);
            yield return null;
        }
        SetInstant(targetAlpha);
    }

    private float GetCurrentAlpha()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i]; if (!r) continue;
            var prop = _colorProps[i];

            var mats = r.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m]; if (!mat) continue;
                if (mat.HasProperty(prop))
                {
                    var c = mat.GetColor(prop);
                    return c.a;
                }
            }
        }
        return 0f;
    }
}
