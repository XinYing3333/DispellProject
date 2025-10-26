using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Player.InteractionSystem;

public class PlayerCollector : MonoBehaviour
{
    [Header("Detect (前方錐形)")]
    public Transform center;               // 玩家胸口/CollectPoint
    public Transform forwardRef;           // 通常用相機或玩家 forward
    public LayerMask interactionMask;
    public float radius = 2.0f;
    [Range(1f, 180f)] public float angle = 90f;

    [Header("Absorb Tween")]
    [SerializeField] private float pullDuration = 0.35f;
    [SerializeField] private float frontOffset  = 0.6f;
    [SerializeField] private Ease  pullEase     = Ease.Linear;

    [Header("Collect Flow")]
    [SerializeField] private bool disablePhysicsDuringPull = true;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = new(0f, 0.85f, 1f, 0.35f);
    [SerializeField] private float gizmoRayLen = 2f;

    private readonly Collider[] _hits = new Collider[64];
    private float _cosHalf;

    // 與控制器溝通（由上層注入）
    private System.Func<bool> _isBusy;
    private System.Action<Rigidbody, bool> _onPulledResult;

    // 狀態
    private readonly HashSet<Transform> _pulling = new();
    private int _activePulls = 0;

    void OnValidate() => _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));
    void Awake()      => _cosHalf = Mathf.Cos(Mathf.Deg2Rad * (angle * 0.5f));

    public void SetBusyChecker(System.Func<bool> f)             => _isBusy = f;
    public void SetOnPulledResult(System.Action<Rigidbody,bool> cb) => _onPulledResult = cb;

    private bool IsCollectorBusy() => (_isBusy != null && _isBusy()) || _activePulls > 0;

    private Vector3 GetFrontPoint()
    {
        Vector3 fwd = forwardRef ? forwardRef.forward : transform.forward;
        return center ? center.position + fwd * frontOffset : transform.position + fwd * frontOffset;
    }

    // —— 單次搜尋→判定→拉近→回報
    public void TryAbsorbOnce()
    {
        if (!center || IsCollectorBusy()) return;

        int n = Physics.OverlapSphereNonAlloc(center.position, radius, _hits, interactionMask, QueryTriggerInteraction.Ignore);

        Transform bestT = null;
        float bestScore = float.MinValue;

        Vector3 fwd = forwardRef ? forwardRef.forward : transform.forward;
        float cosHalf = _cosHalf;

        for (int i = 0; i < n; i++)
        {
            var c = _hits[i];
            if (!c) continue;

            Vector3 to  = c.transform.position - center.position;
            float d2    = to.sqrMagnitude;
            if (d2 < 1e-6f) continue;

            Vector3 toN = to / Mathf.Sqrt(d2);
            if (Vector3.Dot(fwd, toN) < cosHalf) continue;

            float score = 1f / Mathf.Max(0.0001f, Mathf.Sqrt(d2)); // 越近越好
            if (score > bestScore) { bestScore = score; bestT = c.transform; }
        }

        if (!bestT) return;

        var collect = bestT.GetComponentInParent<ICollectable>();
        if (collect != null)
        {
            StartPullThenCollect(bestT, collect);
            return;
        }

        var rb = bestT.GetComponentInParent<Rigidbody>();
        if (rb)
        {
            StartPullThenHandOff(bestT, rb);
        }
    }

    private void StartPullThenCollect(Transform target, ICollectable collectable)
    {
        if (!target || _pulling.Contains(target)) return;
        _pulling.Add(target);
        _activePulls++;

        // 關物理（選配）
        PrepareForPull(target, out var rb, out var cols);

        // 拉到前方一點（不做最終貼手）
        Vector3 end = GetFrontPoint();

        target.DOMove(end, pullDuration)
              .SetEase(pullEase)
              .OnComplete(() =>
              {
                  try
                  {
                      collectable.Collect();
                      _onPulledResult?.Invoke(null, true);
                  }
                  finally
                  {
                      RestoreAfterPull(target, rb, cols);
                      _pulling.Remove(target);
                      _activePulls = Mathf.Max(0, _activePulls - 1);
                  }
              })
              .OnKill(() =>
              {
                  RestoreAfterPull(target, rb, cols);
                  _pulling.Remove(target);
                  _activePulls = Mathf.Max(0, _activePulls - 1);
              });
    }

    // 非 ICollectable：只把剛體拉到玩家前方「附近」，最終貼合交給 HandSlot
    private void StartPullThenHandOff(Transform target, Rigidbody rb)
    {
        if (!target || _pulling.Contains(target)) return;
        _pulling.Add(target);
        _activePulls++;

        Vector3 endPos = GetFrontPoint();

        var origCCD   = rb.collisionDetectionMode;
        var origInterp= rb.interpolation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;

        rb.DOMove(endPos, pullDuration)
          .SetEase(pullEase)
          .SetUpdate(UpdateType.Fixed)
          .OnComplete(() =>
          {
              try
              {
                  _onPulledResult?.Invoke(rb, false);
              }
              finally
              {
                  rb.collisionDetectionMode = origCCD;
                  rb.interpolation          = origInterp;
                  _pulling.Remove(target);
                  _activePulls = Mathf.Max(0, _activePulls - 1);
              }
          })
          .OnKill(() =>
          {
              rb.collisionDetectionMode = origCCD;
              rb.interpolation          = origInterp;
              _pulling.Remove(target);
              _activePulls = Mathf.Max(0, _activePulls - 1);
          });
    }

    // ——— Collect 流程的關/還原物理
    private void PrepareForPull(Transform t, out Rigidbody rb, out Collider[] cols)
    {
        rb   = t.GetComponentInParent<Rigidbody>();
        cols = t.GetComponentsInChildren<Collider>(true);

        if (!disablePhysicsDuringPull) return;
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
            rb.linearVelocity = Vector3.zero;
        }
        foreach (var c in cols) c.enabled = false;
    }

    private void RestoreAfterPull(Transform t, Rigidbody rb, Collider[] cols)
    {
        if (!disablePhysicsDuringPull) return;
        if (!t) return;

        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity  = true;
        }
        foreach (var c in cols)
        {
            if (c) c.enabled = true;
        }
    }

    // —— Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !center) return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(center.position, radius);

        Vector3 origin  = center.position;
        Vector3 forward = forwardRef ? forwardRef.forward : transform.forward;
        float half = angle * 0.5f;

        Gizmos.color = Color.cyan;
        Vector3 leftDir  = Quaternion.AngleAxis(-half, Vector3.up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(+half, Vector3.up) * forward;
        Gizmos.DrawRay(origin, leftDir  * gizmoRayLen);
        Gizmos.DrawRay(origin, rightDir * gizmoRayLen);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, forward * gizmoRayLen);
    }
}
