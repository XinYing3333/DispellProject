using UnityEngine;

public class DoorLevel : MonoBehaviour
{
    [SerializeField] private GameObject door;
    private bool isOpen = false;
    private int objectsInTrigger = 0;

    void Update()
    {
        door.SetActive(!isOpen);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Clone"))
        {
            objectsInTrigger++;
            isOpen = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Clone"))
        {
            objectsInTrigger = Mathf.Max(0, objectsInTrigger - 1);

            if (objectsInTrigger == 0)
                isOpen = false;
        }
    }
}