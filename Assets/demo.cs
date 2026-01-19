using System;
using UnityEngine;

public class demo : MonoBehaviour
{
    public static bool open;
    public GameObject objectToOpen;
    private void Start()
    {
        if (open)
        {
            objectToOpen.SetActive(true);
        }
    }
}
