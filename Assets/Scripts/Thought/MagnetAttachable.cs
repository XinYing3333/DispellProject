using UnityEngine;
using Player.InteractionSystem;

// 只能吸附、不收集的互動物件（例如可被拖著走的箱子、機關等）
public class MagnetAttachable : MonoBehaviour, IMagnetAttachable, IThrowable
{
    private Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    public virtual void OnMagnetAttached(Transform parent)
    {
        if (!rb) return;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.detectCollisions = false;

        // 關鍵修改：保持世界座標（貼在 anchor 位置）
        transform.SetParent(parent, true);
    }


    public virtual void OnMagnetDetached()
    {
        if (!rb) return;
        transform.SetParent(null, true);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
    }

    public virtual void OnBeforeThrow()
    {
        if (!rb) return;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
    }
}