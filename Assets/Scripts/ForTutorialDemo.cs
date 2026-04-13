using DefaultNamespace.Tutorial;
using EventBus.Events.Tutorial;
using UnityEngine;

public class ForTutorialDemo : MonoBehaviour 
{
    [Header("Settings")]
    [SerializeField] private bool sendOnEnable = true;
    [SerializeField] private bool sendOnDisable = false;

    private void OnEnable()
    {
        Raise();
    }

    // private void OnDisable()
    // {
    //     if (sendOnDisable) Raise();
    // }

    public void Raise()
    {
        EventBus<OnTutorialRequirementMet>.Raise(
            new OnTutorialRequirementMet { Requirement = TutorialRequirementType.FirstAdsorb });
        DataManager.Instance.gameData.isFirstAdsorbDone = true;
        DataManager.Instance.CommitSessionData();
    }
}