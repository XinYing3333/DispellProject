using UnityEngine;
using Player;
using System.Collections.Generic;

public class SkillUIManager : MonoBehaviour
{
    [SerializeField] private GameObject radialPanel;

    void Update()
    {
        if (PlayerInputHandler.Instance.IsSkillUIOpen)
        {
            radialPanel.SetActive(true);
        }
        else
        {
            radialPanel.SetActive(false);
        }
    }
    
}
