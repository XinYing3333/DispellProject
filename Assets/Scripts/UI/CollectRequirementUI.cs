
using System;
using EventBus.Events.Collect;
using UnityEngine;
using UnityEngine.UI;

public class CollectRequirementUI : MonoBehaviour
{
    [Header("需求設定")]
    [SerializeField]private CollectionSystem.CollectedType targetType;
    [field:SerializeField]
    private int requiredAmount = 5;
    public Slider progressSlider;
    public GameObject doorToUnlock;

    private bool isUnlocked = false;
    
    private EventBinding<OnDoorCollectStarted> _binding;

    private void Awake()
    {
        _binding = new EventBinding<OnDoorCollectStarted>(SetCurrentRequirement);
    }

    private void Start()
    {
        progressSlider.transform.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        CollectionSystem.OnCollected += HandleCollected;
        EventBus<OnDoorCollectStarted>.Register(_binding);
    }

    private void OnDisable()
    {
        CollectionSystem.OnCollected -= HandleCollected;
        EventBus<OnDoorCollectStarted>.Deregister(_binding);
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
        progressSlider.transform.gameObject.SetActive(false);
    }

    private void SetCurrentRequirement(OnDoorCollectStarted callback)
    {
        SetRequiredAmount(callback.required);
    }

    private void SetRequiredAmount(int e)
    {
        requiredAmount = e;
        progressSlider.value = 0; //數量歸零
        progressSlider.maxValue = requiredAmount;
        progressSlider.transform.gameObject.SetActive(true);

    }
}