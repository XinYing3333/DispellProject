/*
using UnityEngine;
using Player;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class SkillUIManager : MonoBehaviour
{
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject firstButton;

    private bool wasOpen = false;

    void Update()
    {
        bool nowOpen = PlayerInputHandler.Instance.IsSkillUIOpen;

        if (nowOpen && !wasOpen)
        {
            skillPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(firstButton);
            Time.timeScale = 0f;
        }
        else if (!nowOpen && wasOpen)
        {
            skillPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        wasOpen = nowOpen;
    }

    
}
*/
