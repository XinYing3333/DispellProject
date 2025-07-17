using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
public class TriggerGizmoDrawer : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.25f);
    [SerializeField] private Color wireColor = Color.cyan;
    [SerializeField] private string labelText = "Trigger";

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null || !col.enabled) return;

        Gizmos.color = gizmoColor;

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(box.center, box.size);
            DrawLabel(box.center);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            DrawLabel(sphere.center);
        }
        else if (col is CapsuleCollider capsule)
        {
            Vector3 center = capsule.center;
            float radius = capsule.radius;
            float height = capsule.height;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 up = Vector3.up * (height / 2 - radius);
            Gizmos.DrawSphere(center + up, radius);
            Gizmos.DrawSphere(center - up, radius);
            Gizmos.color = wireColor;
            Gizmos.DrawWireSphere(center + up, radius);
            Gizmos.DrawWireSphere(center - up, radius);
            DrawLabel(center);
        }
    }

    private void DrawLabel(Vector3 center)
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        Vector3 worldPos = transform.TransformPoint(center + Vector3.up * 0.5f);
        Handles.Label(worldPos, labelText, style);
    }
#endif

    public void SetLabelText(string text)
    {
        labelText = text;
    }
}
