using UnityEngine;
using System.Collections.Generic;

public class TutorialTestTrigger : MonoBehaviour
{
    [Header("測試設定")]
    [SerializeField] private TutorialData testData; // 拖入你建立的 ScriptableObject
    [SerializeField] private KeyCode testKey = KeyCode.T; // 按下 T 鍵觸發

    [Header("觸發設定")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered = false;

    private void Update()
    {
        // 鍵盤手動測試
        if (Input.GetKeyDown(testKey))
        {
            SendTrigger();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 碰撞觸發測試
        if (triggerOnEnter && other.CompareTag("Player"))
        {
            SendTrigger();
        }
    }

    private void SendTrigger()
    {
        if (testData == null)
        {
            Debug.LogWarning("未指定 TutorialData，請在 Inspector 面板拖入資料。");
            return;
        }

        if (triggerOnlyOnce && hasTriggered) return;

        Debug.Log($"觸發教學: {testData}");
        TutorialManager.Instance.TriggerTutorial(testData);
        hasTriggered = true;
    }
}