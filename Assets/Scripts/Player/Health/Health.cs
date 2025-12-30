// Health.cs

using System;
using System.Collections;
using System.Collections.Generic;
using EventBus.Events.Health;
using Player;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [Header("Hearts")] [Min(1)] public int maxHearts = 4;
    [Min(1)] public int heartSize = 1; // 一顆心代表幾格（預設1）
    [SerializeField] private int current; // 目前總格數（非顆數）

    [Header("Damage Window")] public float invulnDuration = 0.8f; // 受傷後無敵（秒）
    public bool flashOnHit = true; // 受傷閃爍（可接URP材質變數）
    public SkinnedMeshRenderer[] flashTargets;

    [Header("Knockback / Stagger (optional)")]
    public bool enableKnockback = true;

    public float knockbackDuration = 0.2f;
    public AnimationCurve knockbackEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 事件：UI/音效/特效/震動都用這些事件接
    public event Action<int> OnHealed;
    public event Action OnDeath;

    // 內部
    bool _invuln;
    Coroutine _flashCo;
    Rigidbody _rb;
    int MaxTotal => maxHearts * heartSize;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        current = Mathf.Clamp(current == 0 ? MaxTotal : current, 0, MaxTotal);
        EventBus<OnHealthChanged>.Raise(new OnHealthChanged(gameObject, current, MaxTotal));
    }

    // === 對外 API ===
    public void AddHearts(int hearts)
    {
        maxHearts = Mathf.Max(1, maxHearts + hearts);
        ClampAndNotify();
    }

    public void Heal(int amount)
    {
        if (amount > 0)
        {
            current = Mathf.Min(current + amount, MaxTotal);
            OnHealed?.Invoke(amount);
            EventBus<OnHealthChanged>.Raise(new OnHealthChanged(gameObject, current, MaxTotal));
        }
    }

    public void FullHeal()
    {
        current = MaxTotal;
        EventBus<OnHealthChanged>.Raise(new OnHealthChanged(gameObject, current, MaxTotal));
    }

    public int GetCurrent() => current;
    public int GetMax() => MaxTotal;
    public bool IsInvulnerable() => _invuln;

    public void ApplyDamage(DamageInfo info)
    {
        if (current <= 0) return;
        if (_invuln && !info.bypassIFrames) return;

        current = Mathf.Max(0, current - Mathf.Max(1, info.amount));
        EventBus<OnHealthChanged>.Raise(new OnHealthChanged(gameObject, current, MaxTotal));
        
        if (current <= 0)
        {
            Die();
            return;
        }

        if (info.RespawnSafePoint)
        {
            EventBus<OnPlayerRespawn>.Raise(new OnPlayerRespawn());
        }
        
        // 進入短暫無敵
        if (invulnDuration > 0f) StartCoroutine(CoInvuln(invulnDuration));

        // 擊退（可關）
        if (enableKnockback && _rb && info.knockbackForce > 0.01f)
        {
            StartCoroutine(CoKnockback(info.hitDirection.normalized, info.knockbackForce));
        }

        // 受傷閃爍（材質/顏色切換）
        if (flashOnHit && flashTargets != null && flashTargets.Length > 0)
        {
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(CoFlash());
        }
    }

    // === 流程 ===
    void Die()
    {
        EventBus<OnPlayerDeath>.Raise(new OnPlayerDeath());
        
        // 視需要暫時關玩家輸入/碰撞
        /*var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        enabled = false;*/
    }

    IEnumerator CoInvuln(float d)
    {
        _invuln = true;
        float t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _invuln = false;
    }

    IEnumerator CoKnockback(Vector3 dir, float force)
    {
        // 物理或手動位移擇一。這裡用 Impulse + 緩和拉回（簡單手感）
        _rb.AddForce(dir * force, ForceMode.VelocityChange);
        float t = 0f;
        Vector3 start = _rb.linearVelocity;
        while (t < knockbackDuration)
        {
            t += Time.deltaTime;
            float k = knockbackEase.Evaluate(Mathf.Clamp01(t / knockbackDuration));
            // 逐步衰減速度（避免飛太久）
            _rb.linearVelocity = Vector3.Lerp(start, Vector3.zero, k);
            yield return null;
        }
    }

    IEnumerator CoFlash()
    {
        const float total = 0.35f;
        float t = 0f;

        // 快取原始顏色
        var originalColors = new Dictionary<Renderer, Color>();
        foreach (var r in flashTargets)
        {
            if (!r) continue;
            if (r.material.HasProperty("_Color"))
                originalColors[r] = r.material.color;
        }

        while (t < total)
        {
            t += Time.unscaledDeltaTime;

            // 0 / 1 閃爍權重
            float blink = Mathf.PingPong(t * 18f, 1f) > 0.5f ? 1f : 0f;

            foreach (var r in flashTargets)
            {
                if (!r) continue;
                if (!r.material.HasProperty("_Color")) continue;

                Color baseColor = originalColors[r];
                Color flashColor = Color.red;

                // 直接切換（硬閃）
                r.material.color = Color.Lerp(baseColor, flashColor, blink);
            }

            yield return null;
        }

        // 還原顏色
        foreach (var kv in originalColors)
        {
            if (kv.Key)
                kv.Key.material.color = kv.Value;
        }
    }


    void ClampAndNotify()
    {
        current = Mathf.Clamp(current, 0, MaxTotal);
        EventBus<OnHealthChanged>.Raise(new OnHealthChanged(gameObject, current, MaxTotal));
    }
}