using UnityEngine;

public class RigidbodyKinematicController : MonoBehaviour
{
    [SerializeField] bool isKinematic = true;

    Rigidbody[] rigidbodies;

    void Awake()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        ApplyState();
    }

    void OnValidate()
    {
        ApplyState();
    }

    void ApplyState()
    {
        if (rigidbodies == null)
            rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = isKinematic;
        }
    }
}