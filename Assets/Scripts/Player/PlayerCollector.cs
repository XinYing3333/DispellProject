using System;
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

    private void Update()
    {
        if (isCollecting && !isDetectCollect)
        {
            isDetectCollect = true;
            captureParticle.Play();
            captureParticle2.Play();
        }
        else if (!isCollecting && isDetectCollect)
        {
            isDetectCollect = false;
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
            _currentRb.useGravity = true;
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

            Vector3 direction = (collectPoint.position - rb.position).normalized;
            float distance = Vector3.Distance(rb.position, collectPoint.position);

            // 根據距離計算吸力，距離越近吸得越快
            float forceMagnitude = Mathf.Lerp(30f, 100f, 1f - distance / collectRadius);

            rb.AddForce(direction * forceMagnitude);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Acceleration);
        }
    }


    //判斷吸取範圍
    private bool IsInFront(Transform target)
    {
        Vector3 directionToTarget = (target.position - collectPoint.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle < collectAngle * 0.8f;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Collectible"))
        {
            collectParticle.Play();
            CollectionSystem.CollectedType collectedType = other.transform.GetComponent<ThoughtObject>().collectedType;
            CollectionSystem.CollectItem(collectedType);
                
            Debug.Log($"收集了 {collectedType}");
            Destroy(other.gameObject);
        }
    }

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