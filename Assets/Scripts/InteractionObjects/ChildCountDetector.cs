using UnityEngine;

public class ChildCountDetector : MonoBehaviour
{
    public enum DetectionMode
    {
        ChildrenCountZero,    // 判斷子物件數量是否為 0
        AllChildrenDisabled   // 判斷子物件是否都已關閉 (Active Off)
    }

    [Header("Settings")]
    public Transform enemyContainer;
    public DetectionMode detectionMode = DetectionMode.ChildrenCountZero;
    
    [Header("Target Trigger")]
    public GameObject triggerLS;
    [SerializeField] private bool isExternalLS = false;

    private bool isCleared = false;

    private void Awake()
    {
        if (triggerLS != null && !isExternalLS) 
            triggerLS.SetActive(false);
    }

    void Update()
    {
        if (isCleared || enemyContainer == null) return;

        if (CheckConditions())
        {
            ExecuteTrigger();
        }
    }

    private bool CheckConditions()
    {
        switch (detectionMode)
        {
            case DetectionMode.ChildrenCountZero:
                return enemyContainer.childCount == 0;

            case DetectionMode.AllChildrenDisabled:
                // 如果連子物件都沒有，也視為符合關閉條件
                if (enemyContainer.childCount == 0) return true;

                foreach (Transform child in enemyContainer)
                {
                    if (child.gameObject.activeInHierarchy)
                        return false;
                }
                return true;

            default:
                return false;
        }
    }

    private void ExecuteTrigger()
    {
        isCleared = true;
        
        if (isExternalLS)
        {
            var ls = triggerLS.GetComponent<LevelSequenceTrigger>();
            if (ls != null) ls.Play();
        }
        else
        {
            if (triggerLS != null) triggerLS.SetActive(true);
        }
    }
}