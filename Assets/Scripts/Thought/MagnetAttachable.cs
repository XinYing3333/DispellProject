using UnityEngine;
using Player.InteractionSystem;

// 只能吸附、不收集的互動物件（例如可被拖著走的箱子、機關等）
public class MagnetAttachable : MonoBehaviour, IMagnetAttachable, IThrowable
{
    private Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    public void OnMagnetAttached(Transform parent)
    {
        if (!rb) return;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.detectCollisions = false;
        transform.SetParent(parent, false);
    }

    public void OnMagnetDetached()
    {
        if (!rb) return;
        transform.SetParent(null, true);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
    }

    public void OnBeforeThrow()
    {
        if (!rb) return;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
    }
}