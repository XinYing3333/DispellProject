using System.Collections;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("Basic Stats")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float stunDuration = 1.5f;

    private int _currentHealth;
    private bool _isHurt;
    private bool _isDead;

    // 可供 BossController 監聽的事件
    public System.Action OnDamaged;
    public System.Action OnDead;
    
    // 在 BossHealth.cs 中加入以下屬性
    public int MaxHealth => maxHealth;
    public int CurrentHealth => _currentHealth;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    /// <summary>
    /// 被 BossController 或外部攻擊呼叫。
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (_isDead || _isHurt) return;

        _currentHealth -= damage;
        OnDamaged?.Invoke();

        if (_currentHealth <= 0)
        {
            _isDead = true;
            OnDead?.Invoke();
            return;
        }

        // 觸發暫時硬直
        StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        _isHurt = true;
        //OnStunned?.Invoke(); // 這時可播放硬直動畫
        yield return new WaitForSeconds(stunDuration);
        _isHurt = false;
    }

    public bool isHurt => _isHurt;
    public bool IsDead => _isDead;
}