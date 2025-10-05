using UnityEngine;

[DisallowMultipleComponent]
public class Targetable : MonoBehaviour
{
    [Tooltip("瞄準點用的主碰撞器；不填則綜合所有 Collider 的 Bounds 中心")]
    public Collider mainCollider;

    [Header("描邊 / 高亮節點")] [SerializeField] private Outline outlineScript; // 可放 Outline、或任何繼承Behaviour的效果腳本
    [SerializeField] private GameObject outlineObject; // 若你只是用一個外框子物件

    private bool _aimActive;

    private void Awake()
    {
        SetHighLightEnabled(false);
    }

    public Vector3 GetAimPoint()
    {
        if (mainCollider) return mainCollider.bounds.center;
        var cols = GetComponentsInChildren<Collider>();
        if (cols.Length == 0) return transform.position;
        Bounds b = cols[0].bounds;
        for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
        return b.center;
    }

    public void SetAimActive(bool on)
    {
        if (_aimActive == on) return;
        _aimActive = on;

        SetHighLightEnabled(on);
    }

    private void SetHighLightEnabled(bool on)
    {
        if (outlineScript) outlineScript.enabled = on;
        if (outlineObject) outlineObject.SetActive(on);
    }

    private void OnDisable()
    {
        SetHighLightEnabled(false);
    }
}