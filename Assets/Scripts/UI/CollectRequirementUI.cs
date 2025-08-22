using UnityEngine;
using UnityEngine.UI;

public class CollectRequirementUI : MonoBehaviour
{
    [Header("需求設定")]
    public CollectionSystem.CollectedType targetType;
    public int requiredAmount = 5;
    public Slider progressSlider;
    public GameObject doorToUnlock;

    private bool isUnlocked = false;

    private void Start()
    {
        progressSlider.maxValue = requiredAmount;
        progressSlider.value = CollectionSystem.GetItemCount(targetType);
    }

    private void OnEnable()
    {
        CollectionSystem.OnCollected += HandleCollected;
    }

    private void OnDisable()
    {
        CollectionSystem.OnCollected -= HandleCollected;
    }

    private void HandleCollected(CollectionSystem.CollectedType type, int currentAmount)
    {
        if (type != targetType) return;

        progressSlider.value = currentAmount;

        if (!isUnlocked && currentAmount >= requiredAmount)
        {
            UnlockDoor();
        }
    }

    private void UnlockDoor()
    {
        isUnlocked = true;
        if (doorToUnlock != null)
        {
            doorToUnlock.SetActive(false); // 例子：直接關掉門
            Debug.Log("✅ 大門解鎖！");
        }
    }
}