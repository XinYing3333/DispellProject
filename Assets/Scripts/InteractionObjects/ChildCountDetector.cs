using System;
using UnityEngine;

public class ChildCountDetector : MonoBehaviour
{
    public Transform enemyContainer;
    public GameObject triggerLS;
    private bool isCleared = false;


    private void Awake()
    {
        triggerLS.SetActive(false);
    }

    void Update()
    {
        if (isCleared) return;

        // 當父物件下沒有任何子物件時
        if (enemyContainer.childCount == 0)
        {
            isCleared = true;
            TriggerCutscene();
        }
    }

    void TriggerCutscene()
    {
        triggerLS.SetActive(true);
    }
}