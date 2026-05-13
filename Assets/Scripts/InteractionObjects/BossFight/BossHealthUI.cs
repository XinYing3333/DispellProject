using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; 

public class BossHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossHealth bossHealth; 
    [SerializeField] private Image fillImage;       
    [SerializeField] private Image ghostImage;      

    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.3f; 
    [SerializeField] private float ghostDelay = 0.5f;        

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDamaged += UpdateHealthUI;
            bossHealth.OnDead += HandleBossDead;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDamaged -= UpdateHealthUI;
            bossHealth.OnDead -= HandleBossDead;
        }
    }

    private void HandleBossDead()
    {
        // 死亡視同重擊，帶入 true 以執行對應的 UI 震動與最終歸零渲染
        UpdateHealthUI(true); 
    }

    // 參數必須加入 bool 匹配 Action<bool>
    private void UpdateHealthUI(bool isStun)
    {
        float targetFill = (float)bossHealth.CurrentHealth / bossHealth.MaxHealth;

        fillImage.DOFillAmount(targetFill, animationDuration).SetEase(Ease.OutQuad);

        ghostImage.DOFillAmount(targetFill, animationDuration)
            .SetDelay(ghostDelay)
            .SetEase(Ease.InQuad);
        
        // 中止當前動畫，防止連續調用造成 UI 永久偏移
        transform.DOComplete();

        // 依據是否產生硬直決定震動幅度
        if (isStun)
        {
            transform.DOShakePosition(0.2f, 15f); // 重擊震動
        }
        else
        {
            transform.DOShakePosition(0.1f, 5f);  // 輕擊微震
        }
    }
}