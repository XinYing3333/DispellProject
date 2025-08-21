using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerCollector : MonoBehaviour
{
    /*public static SpawnType CurrentSpawnType { get; private set; } // 記錄當前收集的物品類型
    public static Quaternion CurrentSpawnRotation { get; private set; } // 記錄當前收集的物品類型*/

    public float collectRadius = 1f;
    public float collectAngle = 90f;
    public LayerMask collectibleLayer;
    public Transform collectPoint;

    private Rigidbody _currentRb;
    
    private bool isDetectCollect;
    private bool isCollecting;
    [SerializeField] private ParticleSystem captureParticle;
    [SerializeField] private ParticleSystem captureParticle2;
    [SerializeField] private ParticleSystem collectParticle;

    private List<Rigidbody> attractedObjects = new List<Rigidbody>();

    private void Start()
    {
        CollectionSystem.LoadCollection(); // 遊戲開始時讀取收集數據
    }

    private bool hasStartedLoopSFX = false;

    private void Update()
    {
        if (isCollecting && !isDetectCollect)
        {
            isDetectCollect = true;

            if (!hasStartedLoopSFX)
            {
                AudioManager.Instance.PlaySFXLoop(SFXType.Inhale);
                hasStartedLoopSFX = true;
            }

            captureParticle.Play();
            captureParticle2.Play();
        }
        else if (!isCollecting && isDetectCollect)
        {
            isDetectCollect = false;

            AudioManager.Instance.StopSFXLoop();
            hasStartedLoopSFX = false;

            captureParticle.Stop();
            captureParticle2.Stop();
        }
    }


    public void OnCollectCollectibles()
    {
        isCollecting = true;
        FindCollectibles();
        MoveCollectibles();
    }

    public void OnCancelCollect()
    {
        isCollecting = false;

        if (_currentRb != null)
        {
            //_currentRb.useGravity = true;
            _currentRb = null;
        }
    }

    private void FindCollectibles()
    {
        Collider[] collectibles = Physics.OverlapSphere(collectPoint.position, collectRadius, collectibleLayer);

        foreach (Collider collectible in collectibles)
        {
            if (IsInFront(collectible.transform))
            {
                ThoughtObject thoughtObj = collectible.GetComponent<ThoughtObject>();
                if (thoughtObj != null && thoughtObj.isCollectable) // 檢查是否可被收集
                {
                    _currentRb = collectible.GetComponent<Rigidbody>();
                    if (_currentRb != null && !attractedObjects.Contains(_currentRb))
                    {
                        _currentRb.useGravity = false;
                        //rb.linearDamping = 2f;
                        attractedObjects.Add(_currentRb);
                    }
                }
            }
        }
    }


    private void MoveCollectibles()
    {
        for (int i = attractedObjects.Count - 1; i >= 0; i--)
        {
            Rigidbody rb = attractedObjects[i];
            if (rb == null) continue;

            Vector3 toTarget = collectPoint.position - rb.position;
            float distance = toTarget.magnitude;

            // 添加軌道偏移：讓物體沿「抖動曲線」飛行
            Vector3 arcOffset = Vector3.up * Mathf.Sin(Time.time * 10f + i) * 0.2f;

            // 使用 SmoothDamp 模擬黏性吸力感
            Vector3 targetVelocity = toTarget.normalized * Mathf.Lerp(10f, 60f, 1f - distance / collectRadius);
            Vector3 velocity = rb.linearVelocity;
            Vector3 desiredMove = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocity, 0.05f);

            rb.linearVelocity = desiredMove + arcOffset;

            // 自轉
            rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.Force);

            // 判斷是否進入收集距離
            if (distance < 0.4f)
            {
                if (!rb.CompareTag("Collectible")) continue;
                var tc = rb.GetComponent<ThoughtCollectible>();
                if (tc != null)
                {
                    tc.Collect(); // ← 交由 ThoughtCollectible 統一處理：標記、加庫存、回池
                    collectParticle.Play();
                    AudioManager.Instance.PlaySFX(SFXType.Collect);
                    attractedObjects.Remove(rb);
                }
            }
        }
    }
    
    //判斷吸取範圍
    private bool IsInFront(Transform target)
    {
        Vector3 directionToTarget = (target.position - collectPoint.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle < collectAngle * 0.8f;
    }

    /*private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Collectible"))
        {
            if(!isCollecting)return;
            
            var tc = other.gameObject.GetComponent<ThoughtCollectible>();
            if (tc != null)
            {
                tc.Collect(); // ← 統一由 ThoughtCollectible 處理
                collectParticle.Play();
                AudioManager.Instance.PlaySFX(SFXType.Collect);
            }
        }
    }*/
    
#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (collectPoint == null) return;

        // 畫出中心線
        Gizmos.color = Color.green;
        Gizmos.DrawLine(collectPoint.position, collectPoint.position + collectPoint.forward * 2f);

        // 畫出扇形範圍
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 橘色半透明
        DrawViewCone(collectPoint.position, collectPoint.forward, collectAngle * 0.8f, 2f);
    }

// 幫助方法：畫一個視野範圍（扇形）
    private void DrawViewCone(Vector3 origin, Vector3 forward, float angle, float distance)
    {
        int segments = 20;
        float step = angle * 2f / segments;

        Vector3 prevPoint = origin + Quaternion.Euler(0, -angle, 0) * forward * distance;
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -angle + step * i;
            Vector3 nextPoint = origin + Quaternion.Euler(0, currentAngle, 0) * forward * distance;
            Gizmos.DrawLine(origin, nextPoint);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
#endif
}