using System;
using UnityEngine;

public class ButtonPath : MonoBehaviour
{
    public GameObject road;
    private int playerCount = 0;

    private MeshRenderer mesh;
    private Color defaultColor = Color.cyan;
    private Color triggerColor = Color.gray;
    private Vector3 defaultPos;
    private Vector3 triggerPos;

    private void Start()
    {
        defaultPos = transform.position;
        triggerPos = new Vector3(defaultPos.x, defaultPos.y - 0.15f, defaultPos.z);
        mesh = GetComponent<MeshRenderer>();
        road.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsValidActivator(other))
        {
            playerCount++;
            if (playerCount == 1)
            {
                mesh.material.color = triggerColor;
                transform.position = triggerPos;
                SetRoadVisible(true);
            }
        }
    }

    private bool IsValidActivator(Collider other)
    {
        return other.CompareTag("Player") || other.CompareTag("Clone");
    }


    void SetRoadVisible(bool state)
    {
        road.SetActive(state);
    }
}