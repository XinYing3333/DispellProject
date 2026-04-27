using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // 必須引入 DOTween

public class BossHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossHealth bossHealth; // 拖入你的 BossHealth
    [SerializeField] private Image fillImage;       // 即時血條
    [SerializeField] private Image ghostImage;      // 殘影血條

    [Header("Settings")]
    [SerializeField] private float animationDuration = 0.3f; // 變動動畫時間
    [SerializeField] private float ghostDelay = 0.5f;        // 殘影延遲多久開始扣

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            // 監聽 BossHealth 提供的受傷事件
            bossHealth.OnDamaged += UpdateHealthUI;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDamaged -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI()
    {
        // 獲取當前血量比例
        // 註：你可能需要在 BossHealth 加一個 public 屬性獲取當前血量
        float targetFill = (float)bossHealth.CurrentHealth / bossHealth.MaxHealth;

        // 1. 即時血條動畫
        fillImage.DOFillAmount(targetFill, animationDuration).SetEase(Ease.OutQuad);

        // 2. 殘影血條動畫 (先等待一小段時間再追上去)
        ghostImage.DOFillAmount(targetFill, animationDuration)
            .SetDelay(ghostDelay)
            .SetEase(Ease.InQuad);
        
        // 3. 額外視覺效果：血條抖動
        transform.DOShakePosition(0.2f, 10f);
    }
}