using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;
    [SerializeField] private TutorialUI uiPrefab;
    private Queue<TutorialData> tutorialQueue = new Queue<TutorialData>();
    private bool isDisplaying = false;

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TriggerTutorial(TutorialData data)
    {
        if (tutorialQueue.Contains(data)) return; // 避免重複加入
        tutorialQueue.Enqueue(data);
        if (!isDisplaying) ShowNext();
    }

    private void ShowNext()
    {
        if (tutorialQueue.Count == 0)
        {
            isDisplaying = false;
            return;
        }

        isDisplaying = true;
        TutorialData data = tutorialQueue.Dequeue();
        uiPrefab.SetupAndShow(data);
    }

    public void OnTutorialComplete()
    {
        Invoke(nameof(ShowNext), 0.5f); // 轉場間隔
    }
}