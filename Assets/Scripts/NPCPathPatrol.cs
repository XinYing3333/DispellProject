using UnityEngine;

[DisallowMultipleComponent]
public sealed class NPCPathPatrol : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform[] waypoints;

    [Header("Move")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float arriveRadius = 0.2f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool pingPong = false;

    [Header("Wait")]
    [SerializeField] private float waitAtPoint = 0.0f;

    [Header("Rotate")]
    [SerializeField] private bool rotateToMoveDir = true;
    [SerializeField] private float turnSpeedDeg = 540f;

    [Header("Ground Lock (optional)")]
    [SerializeField] private bool keepY = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private int _index = 0;
    private int _dir = 1;
    private float _waitTimer = 0f;

    private void Reset()
    {
        keepY = true;
        rotateToMoveDir = true;
        loop = true;
        pingPong = false;
        arriveRadius = 0.2f;
        moveSpeed = 2.5f;
        turnSpeedDeg = 540f;
        waitAtPoint = 0f;
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (!waypoints[_index]) { AdvanceIndex(); return; }

        if (_waitTimer > 0f)
        {
            _waitTimer -= Time.deltaTime;
            return;
        }

        var targetPos = waypoints[_index].position;
        if (keepY) targetPos.y = transform.position.y;

        var to = targetPos - transform.position;
        var dist = to.magnitude;

        if (dist <= arriveRadius)
        {
            _waitTimer = waitAtPoint;
            AdvanceIndex();
            return;
        }

        var dir = to / Mathf.Max(0.0001f, dist);

        transform.position += dir * (moveSpeed * Time.deltaTime);

        if (rotateToMoveDir && dir.sqrMagnitude > 1e-6f)
        {
            var targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                turnSpeedDeg * Time.deltaTime
            );
        }
    }

    private void AdvanceIndex()
    {
        if (!pingPong)
        {
            _index += _dir;

            if (_index >= waypoints.Length)
            {
                if (!loop) { _index = waypoints.Length - 1; enabled = false; return; }
                _index = 0;
            }
            else if (_index < 0)
            {
                if (!loop) { _index = 0; enabled = false; return; }
                _index = waypoints.Length - 1;
            }
        }
        else
        {
            _index += _dir;

            if (_index >= waypoints.Length)
            {
                _dir = -1;
                _index = Mathf.Clamp(waypoints.Length - 2, 0, waypoints.Length - 1);
            }
            else if (_index < 0)
            {
                _dir = 1;
                _index = Mathf.Clamp(1, 0, waypoints.Length - 1);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!drawGizmos || waypoints == null || waypoints.Length == 0) return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < waypoints.Length; i++)
        {
            var a = waypoints[i];
            if (!a) continue;

            Gizmos.DrawWireSphere(a.position, 0.15f);

            int j = i + 1;
            if (j < waypoints.Length && waypoints[j])
                Gizmos.DrawLine(a.position, waypoints[j].position);
        }

        if (loop && waypoints.Length >= 2 && waypoints[0] && waypoints[^1])
            Gizmos.DrawLine(waypoints[^1].position, waypoints[0].position);
    }
#endif
}
