using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerCollector : MonoBehaviour
{
    // ===== Inspector =====
    [Header("Detect")]
    public float collectRadius = 1f;
    [Range(1f, 180f)] public float collectAngle = 90f;
    public LayerMask interactionMask;
    public Transform collectPoint;

    [Header("FX")]
    [SerializeField] private ParticleSystem captureParticle;
    [SerializeField] private ParticleSystem captureParticle2;
    [SerializeField] private ParticleSystem collectParticle;

    [Header("Tween Settings")]
    public float pullSpeed = 6f;
    public float minDuration = 0.55f;
    public float maxDuration = 1.2f;
    public Ease  pullEase = Ease.OutCubic;
    public float shakeStrength = 0.1f;
    public int   shakeVibrato = 20;

    [Header("Arrival Tuning")]
    [SerializeField] private float preCollectOffset = 0.65f;
    [SerializeField] private float hoverTime       = 0.18f;
    [SerializeField] private float finalSnapTime   = 0.10f;
    [SerializeField] private Ease  finalSnapEase   = Ease.OutQuad;
    [SerializeField] private bool  offsetAlongForward = true;

    [Header("Magnet Mode")]
    [SerializeField] private bool  collectOnCancel   = true;
    [SerializeField] private float magnetRingRadius  = 0.25f;
    [SerializeField] private float magnetHeightOffset = 0f;

    // ===== Types =====
    private class MagnetInfo
    {
        public Rigidbody rb;
        public Transform originalParent;
        public bool origKinematic, origUseGravity, origDetect;
        public ThoughtCollectible tc;

        public RigidbodyConstraints prevConstraints;
        public RigidbodyInterpolation prevInterp;
        public Collider[] cols;
        public bool[]    prevColEnabled;
    }

    private class BodyState
    {
        public bool kinematic, useGravity, detect;
        public RigidbodyConstraints constraints;
        public RigidbodyInterpolation interp;
        public Collider[] cols;
        public bool[]     prevColEnabled;
    }

    // ===== Runtime / Caches =====
    private readonly Collider[] _overlapBuffer = new Collider[64];

    private readonly List<MagnetInfo> _magnetized = new List<MagnetInfo>();
    private readonly HashSet<Rigidbody> _magnetizedSet = new HashSet<Rigidbody>();
    private readonly HashSet<Highlightable> _proxActive = new HashSet<Highlightable>();

    private readonly HashSet<Rigidbody> _attracting = new HashSet<Rigidbody>();
    private readonly Dictionary<Rigidbody, Tween> _tweens = new Dictionary<Rigidbody, Tween>();
    private readonly Dictionary<Rigidbody, ThoughtCollectible> _tcCache = new Dictionary<Rigidbody, ThoughtCollectible>();
    private readonly Dictionary<Rigidbody, BodyState> _flightStates = new Dictionary<Rigidbody, BodyState>();

    private float _cosThreshold;
    private float _proxScanTimer;
    public  float proximityScanInterval = 0.15f;

    private bool isCollecting;
    private bool hasStartedLoopSFX;
    private Coroutine _scanRoutine;

    // ===== Unity =====
    private void Start()
    {
        CollectionSystem.LoadCollection();
        _cosThreshold = Mathf.Cos(Mathf.Deg2Rad * (collectAngle * 0.5f));
    }

    private void OnValidate()
    {
        _cosThreshold = Mathf.Cos(Mathf.Deg2Rad * (collectAngle * 0.5f));
    }

    private void Update()
    {
        _proxScanTimer += Time.deltaTime;
        if (_proxScanTimer >= proximityScanInterval)
        {
            _proxScanTimer = 0f;
            ProximityScanAndHighlight();
        }

        if (isCollecting && !hasStartedLoopSFX)
        {
            AudioManager.Instance.PlaySFXLoop(SFXType.Inhale);
            hasStartedLoopSFX = true;
            ToggleInhaleVFX(true);
            if (_scanRoutine == null) _scanRoutine = StartCoroutine(ScanLoop());
        }
        else if (!isCollecting && hasStartedLoopSFX)
        {
            StopLoopSFXIfIdle();
            ToggleInhaleVFX(false);
            if (_scanRoutine != null) { StopCoroutine(_scanRoutine); _scanRoutine = null; }
        }
    }

    private void OnDisable()
    {
        foreach (var h in _proxActive) if (h) h.SetProximityHighlight(false);
        _proxActive.Clear();
    }

    // ===== Highlighting =====
    private void ProximityScanAndHighlight()
    {
        if (collectPoint == null) return;

        int count = Physics.OverlapSphereNonAlloc(
            collectPoint.position, collectRadius, _overlapBuffer,
            interactionMask, QueryTriggerInteraction.Ignore
        );

        Vector3 forward = collectPoint.forward;
        var newSet = new HashSet<Highlightable>();

        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (!col) continue;

            Vector3 to = col.transform.position - collectPoint.position;
            float d2 = to.sqrMagnitude;
            if (d2 < 1e-6f) continue;
            to /= Mathf.Sqrt(d2);

            if (Vector3.Dot(forward, to) < _cosThreshold) continue;

            var h = col.GetComponentInParent<Highlightable>();
            if (!h) continue;

            bool isInteractable = col.GetComponentInParent<Collectible>() ||
                                  col.GetComponentInParent<Targetable>();
            if (!isInteractable) continue;

            newSet.Add(h);
        }

        foreach (var h in _proxActive)
            if (h && !newSet.Contains(h))
                h.SetProximityHighlight(false);

        foreach (var h in newSet)
            if (!_proxActive.Contains(h))
                h.SetProximityHighlight(true);

        _proxActive.Clear();
        foreach (var h in newSet) _proxActive.Add(h);
    }

    private void ToggleInhaleVFX(bool on)
    {
        if (captureParticle)
        {
            var e = captureParticle.emission;
            e.enabled = on;
            if (on && !captureParticle.isPlaying) captureParticle.Play();
        }
        if (captureParticle2)
        {
            var e2 = captureParticle2.emission;
            e2.enabled = on;
            if (on && !captureParticle2.isPlaying) captureParticle2.Play();
        }
        if (!on)
        {
            captureParticle?.Stop();
            captureParticle2?.Stop();
        }
    }

    // ===== Input Entrypoints =====
    public void OnCollectCollectibles()
    {
        isCollecting = true;
    }

    public void OnCancelCollect()
    {
        isCollecting = false;

        // 停掉所有正在拉動的 tween（不觸發 OnComplete）
        var keys = new List<Rigidbody>(_tweens.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            var rbKey = keys[i];
            if (_tweens.TryGetValue(rbKey, out var tw) && tw.IsActive()) tw.Kill(false);
            if (rbKey) rbKey.isKinematic = false;
        }
        _tweens.Clear();
        _attracting.Clear();

        // 釋放或收集吸附中的物件，並完整還原
        for (int i = 0; i < _magnetized.Count; i++)
        {
            var m = _magnetized[i];
            if (m == null || !m.rb) continue;

            m.rb.transform.SetParent(m.originalParent, true);

            if (collectOnCancel && m.tc != null)
            {
                m.tc.Collect();
                collectParticle?.Play();
                AudioManager.Instance.PlaySFX(SFXType.Collect);
            }
            else
            {
                RestoreRigidbody(m.rb, m.origKinematic, m.origUseGravity, m.origDetect, m.prevInterp, m.prevConstraints);
                RestoreColliders(m.cols, m.prevColEnabled);
            }
        }

        _magnetized.Clear();
        _magnetizedSet.Clear();
        _tcCache.Clear();
    }

    // ===== Collect Scan / Tween =====
    private System.Collections.IEnumerator ScanLoop()
    {
        var wait = new WaitForSeconds(0.12f);
        while (isCollecting)
        {
            ScanAndBeginAttract();
            yield return wait;
        }
        _scanRoutine = null;
    }

    private void ScanAndBeginAttract()
    {
        if (!collectPoint) return;

        int count = Physics.OverlapSphereNonAlloc(
            collectPoint.position, collectRadius, _overlapBuffer, interactionMask
        );

        var forward = collectPoint.forward;

        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (!col) continue;

            Vector3 dir = col.transform.position - collectPoint.position;
            float d2 = dir.sqrMagnitude;
            if (d2 < 1e-6f) continue;
            dir /= Mathf.Sqrt(d2);

            if (Vector3.Dot(forward, dir) < _cosThreshold) continue;

            if (!col.TryGetComponent(out Rigidbody rb)) continue;
            if (_magnetizedSet.Contains(rb)) continue;
            if (_attracting.Contains(rb)) continue;

            col.TryGetComponent(out ThoughtCollectible tc);
            bool canCollect    = tc != null && TryIsCollectable(col.transform);
            bool canMagnetOnly = tc == null && col.GetComponentInParent<MagnetAttachable>() != null;
            if (!canCollect && !canMagnetOnly) continue;

            _attracting.Add(rb);
            if (tc != null) _tcCache[rb] = tc;

            BeginAttract(rb);
        }
    }

    private void AttachToMagnet(Rigidbody rb)
    {
        if (!rb) return;

        var info = new MagnetInfo
        {
            rb = rb,
            originalParent = rb.transform.parent,
            tc = _tcCache.TryGetValue(rb, out var cachedTc) ? cachedTc : rb.GetComponent<ThoughtCollectible>()
        };

        // 若從飛行進來，沿用飛行前的原狀態；否則即時快照
        if (_flightStates.TryGetValue(rb, out var bs))
        {
            info.origKinematic   = bs.kinematic;
            info.origUseGravity  = bs.useGravity;
            info.origDetect      = bs.detect;
            info.prevConstraints = bs.constraints;
            info.prevInterp      = bs.interp;
            info.cols            = bs.cols;
            info.prevColEnabled  = bs.prevColEnabled;
            _flightStates.Remove(rb);
        }
        else
        {
            SnapshotColliders(rb, out info.cols, out info.prevColEnabled);
            info.origKinematic   = rb.isKinematic;
            info.origUseGravity  = rb.useGravity;
            info.origDetect      = rb.detectCollisions;
            info.prevConstraints = rb.constraints;
            info.prevInterp      = rb.interpolation;
            SetCollidersEnabled(info.cols, false);
        }

        // 殺掉殘留 tween、凍結、掛到收集點
        DOTween.Kill(rb, false);
        DOTween.Kill(rb.transform, false);
        ClearMotion(rb);
        rb.isKinematic      = true;
        rb.useGravity       = false;
        rb.detectCollisions = false;
        rb.interpolation    = RigidbodyInterpolation.None;
        rb.constraints      = RigidbodyConstraints.FreezeAll;

        rb.transform.SetParent(collectPoint, false);

        _magnetized.Add(info);
        _magnetizedSet.Add(rb);
        ArrangeMagnetized();
    }

    private void ArrangeMagnetized()
    {
        int n = _magnetized.Count;
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            var info = _magnetized[i];
            if (info == null || !info.rb) continue;

            float ang = (360f / n) * i;
            Vector3 localPos = Quaternion.Euler(0f, ang, 0f) * (Vector3.forward * magnetRingRadius);
            localPos.y += magnetHeightOffset;

            info.rb.transform.localPosition = localPos;
            info.rb.transform.localRotation = Quaternion.identity;
        }
    }

    private bool TryIsCollectable(Transform t)
    {
        var thoughtObj = t.GetComponent<ThoughtObject>();
        return thoughtObj != null && thoughtObj.isCollectable;
    }

    private void BeginAttract(Rigidbody rb)
    {
        if (!rb) return;

        // 起飛前快照 + 關碰撞（避免推玩家）
        var bs = new BodyState
        {
            kinematic   = rb.isKinematic,
            useGravity  = rb.useGravity,
            detect      = rb.detectCollisions,
            constraints = rb.constraints,
            interp      = rb.interpolation
        };
        SnapshotColliders(rb, out bs.cols, out bs.prevColEnabled);
        SetCollidersEnabled(bs.cols, false);
        _flightStates[rb] = bs;

        ClearMotion(rb);
        rb.isKinematic      = true;
        rb.useGravity       = false;
        rb.detectCollisions = false;
        rb.interpolation    = RigidbodyInterpolation.None;

        if (_tweens.TryGetValue(rb, out var oldTween))
        {
            if (oldTween.IsActive()) oldTween.Kill(false);
            _tweens.Remove(rb);
        }

        Vector3 cpPos = collectPoint.position;
        Vector3 cpFwd = collectPoint.forward;
        Vector3 midTarget = offsetAlongForward ? cpPos - cpFwd * preCollectOffset : cpPos;

        float distToMid = Vector3.Distance(rb.position, midTarget);
        float d1 = Mathf.Clamp(distToMid / Mathf.Max(0.007f, pullSpeed), minDuration, maxDuration);

        var seq = DOTween.Sequence().SetUpdate(UpdateType.Fixed);

        seq.Append(rb.DOMove(midTarget, d1).SetEase(pullEase));

        if (hoverTime > 0.001f)
        {
            if (shakeStrength > 0f)
                seq.Join(rb.transform.DOShakePosition(d1, shakeStrength, shakeVibrato, 90, false, true).SetUpdate(UpdateType.Fixed));
            seq.AppendInterval(hoverTime);
        }

        if (offsetAlongForward)
            seq.Append(rb.DOMove(cpPos, finalSnapTime).SetEase(finalSnapEase));
        
        seq.OnComplete(() =>
        {
            // 移除 tween/拉動狀態
            _tweens.Remove(rb);
            _attracting.Remove(rb);

            // 這顆如果是「可收集的念頭」，到點就直接收集，不進入吸附模式
            if (_tcCache.TryGetValue(rb, out var tc) && tc != null)
            {
                // 立刻收集
                tc.Collect();
                collectParticle?.Play();
                AudioManager.Instance.PlaySFX(SFXType.Collect);

                // 清理：這顆物件完成生命周期了，不需要還原飛行期狀態
                _tcCache.Remove(rb);
                _flightStates.Remove(rb);     // 釋出快照記錄
                _magnetizedSet.Remove(rb);    // 保險：不應該在這裡，但確保不殘留
                return;
            }
            ToggleInhaleVFX(false);
            // 否則：不是可收集的念頭 → 進入吸附，跟著玩家移動，等待取消
            AttachToMagnet(rb);
            _tcCache.Remove(rb); // 若不是 Collectible，這裡通常是 null，但清掉以防殘留
        });

        seq.OnKill(() =>
        {
            // 飛行途中被取消：完整還原飛行前狀態
            if (rb && _flightStates.TryGetValue(rb, out var s))
            {
                RestoreRigidbody(rb, s.kinematic, s.useGravity, s.detect, s.interp, s.constraints);
                RestoreColliders(s.cols, s.prevColEnabled);
                _flightStates.Remove(rb);
            }
            _tweens.Remove(rb);
            _attracting.Remove(rb);
            _tcCache.Remove(rb);
        });

        seq.SetLink(rb.gameObject, LinkBehaviour.KillOnDestroy);
        _tweens[rb] = seq;
    }

    private void StopLoopSFXIfIdle()
    {
        if (hasStartedLoopSFX)
        {
            AudioManager.Instance.StopSFXLoop();
            hasStartedLoopSFX = false;
        }
    }

    // ===== Utilities =====
    private static void ClearMotion(Rigidbody rb)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private static void SnapshotColliders(Component root, out Collider[] cols, out bool[] wasEnabled)
    {
        cols = root.GetComponentsInChildren<Collider>(true);
        wasEnabled = new bool[cols.Length];
        for (int i = 0; i < cols.Length; i++)
        {
            if (!cols[i]) continue;
            wasEnabled[i] = cols[i].enabled;
        }
    }

    private static void SetCollidersEnabled(Collider[] cols, bool enabled)
    {
        if (cols == null) return;
        for (int i = 0; i < cols.Length; i++)
            if (cols[i]) cols[i].enabled = enabled;
    }

    private static void RestoreColliders(Collider[] cols, bool[] prev)
    {
        if (cols == null) return;
        for (int i = 0; i < cols.Length; i++)
            if (cols[i]) cols[i].enabled = (prev != null && i < prev.Length) ? prev[i] : true;
    }

    private static void RestoreRigidbody(
        Rigidbody rb,
        bool kinematic, bool useGravity, bool detect,
        RigidbodyInterpolation interp, RigidbodyConstraints constraints)
    {
        rb.isKinematic      = kinematic;
        rb.useGravity       = useGravity;
        rb.detectCollisions = detect;
        rb.interpolation    = interp;
        rb.constraints      = constraints;
        ClearMotion(rb);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!collectPoint) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(collectPoint.position, collectPoint.position + collectPoint.forward * 2f);

        Gizmos.color = Color.green;
        DrawViewCone(collectPoint.position, collectPoint.forward, collectAngle * 0.5f, 2f);
    }

    private void DrawViewCone(Vector3 origin, Vector3 forward, float halfAngle, float distance)
    {
        int segments = 20;
        float step = (halfAngle * 2f) / segments;
        Vector3 prev = origin + Quaternion.Euler(0, -halfAngle, 0) * forward * distance;
        for (int i = 1; i <= segments; i++)
        {
            float ang = -halfAngle + step * i;
            Vector3 next = origin + Quaternion.Euler(0, ang, 0) * forward * distance;
            Gizmos.DrawLine(origin, next);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
