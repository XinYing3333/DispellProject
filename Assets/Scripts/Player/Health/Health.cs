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

    [Header("Damage Window")] 
    public float invulnDuration = 0.8f; // 受傷後無敵（秒）

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
        
        EventBus<OnPlayerDamaged>.Raise(new OnPlayerDamaged());

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
    }

    // === 流程 ===
    void Die()
    {
        EventBus<OnPlayerDeath>.Raise(new OnPlayerDeath());
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
        // 1. 暫時鎖定玩家輸入（若你有 InputManager，請在此處禁用）
        // playerInput.enabled = false; 
        PlayerInputHandler.Instance.SetLockMovement(true);

        // 2. 清除當前殘留速度，確保擊退力道一致
        _rb.linearVelocity = Vector3.zero;

        // 3. 施加一次性的物理衝量
        // 使用 Impulse 模式，這會根據物體質量產生自然的位移
        _rb.AddForce(dir * force, ForceMode.Impulse);

        // 4. 等待擊退持續時間
        // 此期間不進行任何手動速度修改，讓物理引擎的 Drag 處理減速
        float t = 0f;
        while (t < knockbackDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        PlayerInputHandler.Instance.SetLockMovement(false);

        // 5. 恢復輸入與狀態
        // playerInput.enabled = true;
    }
    
    void ClampAndNotify()
    {
        current = Mathf.Clamp(current, 0, MaxTotal);
        EventBus<OnHealthChanged>.Raise(new OnHealthChanged(gameObject, current, MaxTotal));
    }
}