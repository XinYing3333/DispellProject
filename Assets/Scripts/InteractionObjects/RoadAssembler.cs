using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EventBus.Events.Tutorial;
using DefaultNamespace.Tutorial; // 確保引用 Requirement 枚舉命名空間

public class RoadAssembler : MonoBehaviour
{
    [Header("觸發條件")]
    public TutorialRequirementType triggerType = TutorialRequirementType.FirstCollectEnemy;

    [Header("動畫參數")]
    public float duration = 1.5f;
    public float scatterRange = 20f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private struct PartData
    {
        public Transform transform;
        public Vector3 targetPos;
        public Vector3 startPos;
    }

    private List<PartData> roadParts = new List<PartData>();
    private bool isAssembled = false;
    private EventBinding<OnTutorialRequirementMet> _binding;

    private void OnEnable()
    {
        // 綁定時指向帶有參數的方法
        _binding = new EventBinding<OnTutorialRequirementMet>(AssembleRoad);
        EventBus<OnTutorialRequirementMet>.Register(_binding);
    }

    private void OnDisable()
    {
        EventBus<OnTutorialRequirementMet>.Deregister(_binding);
    }

    void Awake()
    {
        foreach (Transform child in transform)
        {
            PartData data = new PartData
            {
                transform = child,
                targetPos = child.localPosition,
                startPos = child.localPosition + (Random.insideUnitSphere * scatterRange)
            };
            roadParts.Add(data);
            child.localPosition = data.startPos;
            child.gameObject.SetActive(false);
        }
    }

    // 接收事件實體並檢查 Requirement
    public void AssembleRoad(OnTutorialRequirementMet eventData)
    {
        if (isAssembled) return;

        // 核心檢查
        if (eventData.Requirement == triggerType)
        {
            isAssembled = true;
            foreach (var part in roadParts)
            {
                part.transform.gameObject.SetActive(true);
            }
            StartCoroutine(AnimateAssembly());
        }
    }

    IEnumerator AnimateAssembly()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);
            foreach (var part in roadParts)
            {
                part.transform.localPosition = Vector3.Lerp(part.startPos, part.targetPos, t);
            }
            yield return null;
        }
        foreach (var part in roadParts)
        {
            part.transform.localPosition = part.targetPos;
        }
    }
}