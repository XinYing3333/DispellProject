using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // <-- DOTween
using Random = UnityEngine.Random;

public class PlayerCollector : MonoBehaviour
{
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
    public float pullSpeed = 6f;             // 距離越遠，時間越長（SetSpeedBased）
    public float minDuration = 0.55f;        // 最短保底
    public float maxDuration = 1.2f;         // 最長保底
    public Ease pullEase = Ease.OutCubic;    // 或改成 AnimationCurve
    public float shakeStrength = 0.1f;      // 抖動強度
    public int shakeVibrato = 20;            // 抖動頻率
    
    [Header("Arrival Tuning")]
    [SerializeField] private float preCollectOffset = 0.65f; // 在收集點前方停下來的距離(單位：m)
    [SerializeField] private float hoverTime       = 0.18f;  // 停留/展示時間
    [SerializeField] private float finalSnapTime   = 0.1f;  // 最後吸入的時間
    [SerializeField] private Ease  finalSnapEase   = Ease.OutQuad; // 最後吸入手感
    [SerializeField] private bool  offsetAlongForward = true; // 依 collectPoint.forward 退後
    
    private readonly Collider[] _overlapBuffer = new Collider[64];

    // 近距離高亮名單
    private readonly HashSet<Highlightable> _proxActive = new HashSet<Highlightable>();

    // 角度判斷預算
    private float _cosThreshold;

    private float _proxScanTimer;
    public float proximityScanInterval = 0.15f;                 

    private bool isCollecting;
    private bool hasStartedLoopSFX = false;
    
    // 快取資料結構
    private readonly HashSet<Rigidbody> _attracting = new HashSet<Rigidbody>();
    private readonly Dictionary<Rigidbody, Tween> _tweens = new Dictionary<Rigidbody, Tween>();
    private readonly Dictionary<Rigidbody, ThoughtCollectible> _tcCache = new Dictionary<Rigidbody, ThoughtCollectible>();
    
    private void Start()
    {
        CollectionSystem.LoadCollection();
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

        // ② 吸收：沿用你原來的開關與掃描邏輯
        if (isCollecting && !hasStartedLoopSFX)
        {
            AudioManager.Instance.PlaySFXLoop(SFXType.Inhale);
            hasStartedLoopSFX = true;
            ToggleSuckVFX(true);
            if (_scanRoutine == null) _scanRoutine = StartCoroutine(ScanLoop());
        }
        else if (!isCollecting && hasStartedLoopSFX)
        {
            StopLoopSFXIfIdle();
            ToggleSuckVFX(false);
            if (_scanRoutine != null) { StopCoroutine(_scanRoutine); _scanRoutine = null; }
        }/*
        // 邊界觸發：音效/粒子
        if (isCollecting && !hasStartedLoopSFX)
        {
            AudioManager.Instance.PlaySFXLoop(SFXType.Inhale);
            hasStartedLoopSFX = true;
            ToggleSuckVFX(true);
            // 啟動降頻掃描
            if (_scanRoutine == null) _scanRoutine = StartCoroutine(ScanLoop());
        }
        else if (!isCollecting && hasStartedLoopSFX)
        {
            StopLoopSFXIfIdle();
            ToggleSuckVFX(false);
            // 停止掃描
            if (_scanRoutine != null) { StopCoroutine(_scanRoutine); _scanRoutine = null; }
        }*/
    }
    
    private void OnDisable()
    {
        // 關掉所有近距離高亮，避免物件被 disable 時殘留
        foreach (var h in _proxActive) if (h) h.SetProximityHighlight(false);
        _proxActive.Clear();
    }

    
    private void ProximityScanAndHighlight()
    {
        if (collectPoint == null) return;

        int count = Physics.OverlapSphereNonAlloc(
            collectPoint.position,
            collectRadius,
            _overlapBuffer,
            interactionMask,                                        // ← 用互動層粗篩
            QueryTriggerInteraction.Ignore
        );

        Vector3 forward = collectPoint.forward;
        var newSet = new HashSet<Highlightable>();

        for (int i = 0; i < count; i++)
        {
            var col = _overlapBuffer[i];
            if (!col) continue;

            // 角度扇形判斷
            Vector3 to = (col.transform.position - collectPoint.position);
            float d2 = to.sqrMagnitude;
            if (d2 < 1e-6f) continue;
            to /= Mathf.Sqrt(d2);
            if (Vector3.Dot(forward, to) < _cosThreshold) continue;

            // 找有 Highlightable，且具備 Collectible 或 Targetable 任一
            var h = col.GetComponentInParent<Highlightable>();
            if (!h) continue;

            bool isInteractable = col.GetComponentInParent<Collectible>() ||
                                  col.GetComponentInParent<Targetable>();
            if (!isInteractable) continue;

            newSet.Add(h);
        }

        // 關掉離開範圍的
        foreach (var h in _proxActive)
            if (h && !newSet.Contains(h))
                h.SetProximityHighlight(false);

        // 開啟新進範圍的
        foreach (var h in newSet)
            if (!_proxActive.Contains(h))
                h.SetProximityHighlight(true);

        _proxActive.Clear();
        foreach (var h in newSet) _proxActive.Add(h);
    }


    private void ToggleSuckVFX(bool on)
    {
        if (captureParticle != null)
        {
            var e = captureParticle.emission;
            e.enabled = on;
            if (on && !captureParticle.isPlaying) captureParticle.Play();
        }
        if (captureParticle2 != null)
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

    public void OnCollectCollectibles()
    {
        isCollecting = true;
        // 其餘交由 ScanLoop 週期掃描
    }

    /*public void OnCancelCollect()
    {
        isCollecting = false;
        // 結束所有 tween，恢復剛體
        foreach (var kv in _tweens)
        {
            if (kv.Value.IsActive()) kv.Value.Kill(false);
            var rb = kv.Key;
            if (rb != null) rb.isKinematic = false;
        }
        _tweens.Clear();
        _tcCache.Clear();
        _attracting.Clear();
    }*/
    public void OnCancelCollect()
    {
        isCollecting = false;

        // 結束吸附 tween，恢復剛體（保留）
        foreach (var kv in _tweens)
        {
            if (kv.Value.IsActive()) kv.Value.Kill(false);
            var rb = kv.Key;
            if (rb != null) rb.isKinematic = false;
        }
        _tweens.Clear();
        _tcCache.Clear();
        _attracting.Clear();

        // ⚠️ 不要清 _proxActive，不要關掉 ProximityHighlight（讓靠近的物件仍維持近距離高亮）
    }


    private Coroutine _scanRoutine;

    // 降頻掃描避免每禎 OverlapSphere
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
        if (collectPoint == null) return;

        int count = Physics.OverlapSphereNonAlloc(
            collectPoint.position,
            collectRadius,
            _overlapBuffer,
            interactionMask                                       // ← 改用互動層
        );

        var forward = collectPoint.forward;

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null) continue;

            Vector3 dir = (col.transform.position - collectPoint.position);
            float distSqr = dir.sqrMagnitude;
            if (distSqr < 0.0001f) continue;
            dir /= Mathf.Sqrt(distSqr);

            if (Vector3.Dot(forward, dir) < _cosThreshold) continue;

            if (!col.TryGetComponent<Rigidbody>(out var rb)) continue;
            if (_attracting.Contains(rb)) continue;

            // 仍然只吸「可被收集」的（沿用你現有細篩）
            if (!col.TryGetComponent<ThoughtCollectible>(out var tc)) continue;
            if (!TryIsCollectable(col.transform)) continue;

            _attracting.Add(rb);
            _tcCache[rb] = tc;
            BeginAttract(rb);
        }
    }

    /*
    private void ScanAndBeginAttract()
    {
        if (collectPoint == null) return;

        int count = Physics.OverlapSphereNonAlloc(
            collectPoint.position, 
            collectRadius, 
            _overlapBuffer, 
            collectibleLayer
        );

        var forward = collectPoint.forward;

        for (int i = 0; i < count; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col == null) continue;

            Vector3 dir = (col.transform.position - collectPoint.position);
            float distSqr = dir.sqrMagnitude;
            if (distSqr < 0.0001f) continue;
            dir /= Mathf.Sqrt(distSqr);

            // dot 角度判斷
            if (Vector3.Dot(forward, dir) < _cosThreshold) continue;

            if (!col.TryGetComponent<Rigidbody>(out var rb)) continue;
            if (_attracting.Contains(rb)) continue;

            // 只要能被收集就開始吸
            if (!col.TryGetComponent<ThoughtCollectible>(out var tc)) continue;
            if (!TryIsCollectable(col.transform)) continue;

            _attracting.Add(rb);
            _tcCache[rb] = tc;
            BeginAttract(rb);
        }
    }*/

    // 你原本的 ThoughtObject.isCollectable 判斷（這裡包成方法，方便替換邏輯）
    private bool TryIsCollectable(Transform t)
    {
        var thoughtObj = t.GetComponent<ThoughtObject>();
        return thoughtObj != null && thoughtObj.isCollectable;
    }

    private void BeginAttract(Rigidbody rb)
{
    if (rb == null) return;

    if (_tweens.TryGetValue(rb, out var oldTween))
    {
        if (oldTween.IsActive()) oldTween.Kill(false);
        _tweens.Remove(rb);
    }

    // 目標位置：先到「面前的中繼點」，再進行最終吸入
    Vector3 cpPos = collectPoint.position;
    Vector3 cpFwd = collectPoint.forward;
    Vector3 midTarget = offsetAlongForward
        ? cpPos - cpFwd * preCollectOffset   // 在你面前「退後一點」停住
        : cpPos;                              // 如果不想要中繼點，直接設為 cpPos

    float distToMid = Vector3.Distance(rb.position, midTarget);
    float d1 = Mathf.Clamp(distToMid / Mathf.Max(0.007f, pullSpeed), minDuration, maxDuration);

    // 進入吸附期，避免物理干擾
    bool origKinematic = rb.isKinematic;
    rb.isKinematic = true;

    var seq = DOTween.Sequence().SetUpdate(UpdateType.Fixed);

    // 第一段：拉到中繼點（看得到飛到你面前）
    var move1 = rb.DOMove(midTarget, d1).SetEase(pullEase);
    seq.Append(move1);

    // 中繼停留（可選：小抖動展示）
    if (hoverTime > 0.001f)
    {
        // 用 transform 抖，不影響剛體到位
        if (shakeStrength > 0f)
            seq.Join(rb.transform
                .DOShakePosition(d1, shakeStrength, shakeVibrato, 90, false, true)
                .SetUpdate(UpdateType.Fixed));

        // 停一下（用 AppendInterval 讓玩家看清楚有聚到面前）
        seq.AppendInterval(hoverTime);
    }

    // 第二段：短促吸入到真正收集點
    if (offsetAlongForward)
    {
        var move2 = rb.DOMove(cpPos, finalSnapTime).SetEase(finalSnapEase);
        seq.Append(move2);
    }

    // 完成後才 Collect
    seq.OnComplete(() =>
    {
        if (_tcCache.TryGetValue(rb, out var tc) && tc != null)
        {
            tc.Collect();
            collectParticle?.Play();
            AudioManager.Instance.PlaySFX(SFXType.Collect);
        }

        rb.isKinematic = false;
        _tweens.Remove(rb);
        _attracting.Remove(rb);
        _tcCache.Remove(rb);

        if (_attracting.Count == 0 && !isCollecting)
        {
            StopLoopSFXIfIdle();
            ToggleSuckVFX(false);
        }
    });

    seq.OnKill(() =>
    {
        if (rb != null) rb.isKinematic = false;
        _tweens.Remove(rb);
        _attracting.Remove(rb);
        _tcCache.Remove(rb);
    });

    // 連結生命週期（物件回池/刪除自動 Kill）
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (collectPoint == null) return;
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
