using UnityEngine;

public class AirflowZone : MonoBehaviour
{
    [Header("噴射參數")]
    public float upwardForce = 15f;
    public ForceMode forceMode = ForceMode.VelocityChange;
    public string[] validTags = { "Player", "Clone", "Object" };

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidTarget(other)) return;

        if (other.attachedRigidbody != null)
        {
            other.attachedRigidbody.AddForce(Vector3.up * upwardForce, forceMode);
        }
    }

    private bool IsValidTarget(Collider col)
    {
        foreach (var tag in validTags)
        {
            if (col.CompareTag(tag)) return true;
        }
        return false;
    }
}