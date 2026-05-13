using System;
using System.Collections;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("Basic Stats")]
    [SerializeField] private int maxHealth = 40;
    [SerializeField] private float invulnerabilityDuration = 0.5f; // 新增：非硬直狀態下的基礎無敵時間
    [SerializeField] private float stunDuration = 1.5f;

    private int _currentHealth;
    private bool _isHurt;
    private bool _isDead;

    // 變更：傳遞 bool 告知外部是否觸發硬直
    public Action<bool> OnDamaged;
    public Action OnDead;
    
    public int MaxHealth => maxHealth;
    public int CurrentHealth => _currentHealth;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(int damage, bool isStun = true)
    {
        if (_isDead || _isHurt) return;

        _currentHealth -= damage;

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            _isDead = true;
            OnDead?.Invoke();
            return;
        }

        OnDamaged?.Invoke(isStun);
        StartCoroutine(DamageCooldownRoutine(isStun));
    }

    private IEnumerator DamageCooldownRoutine(bool isStun)
    {
        _isHurt = true;
        
        // 依據是否產生硬直，決定鎖定狀態的持續時間
        float waitTime = isStun ? stunDuration : invulnerabilityDuration;
        yield return new WaitForSeconds(waitTime);
        
        _isHurt = false;
    }

    public bool isHurt => _isHurt;
    public bool IsDead => _isDead;
}