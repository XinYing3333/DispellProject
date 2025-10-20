using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [SerializeField] private Color pathColor = Color.green;
    public Transform GetWaypoint(int waypointIndex)
    {
        return transform.GetChild(waypointIndex);
    }

    public int GetNextWaypointIndex(int currentWaypointIndex)
    {
        int nextWaypointIndex = currentWaypointIndex + 1;

        if (nextWaypointIndex == transform.childCount)
        {
            nextWaypointIndex = 0;
        }

        return nextWaypointIndex;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = pathColor;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform current = transform.GetChild(i);
            Transform next = transform.GetChild((i + 1) % transform.childCount);

            if (current != null && next != null)
            {
                Gizmos.DrawSphere(current.position, 0.2f);
                Gizmos.DrawLine(current.position, next.position);
            }

            // 顯示編號
            UnityEditor.Handles.Label(current.position + Vector3.up * 0.3f, $"WP {i}");
        }
    }
#endif
}