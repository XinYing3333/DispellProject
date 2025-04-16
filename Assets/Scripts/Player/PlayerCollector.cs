using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerCollector : MonoBehaviour
{
    public static SpawnType CurrentSpawnType { get; private set; } // 記錄當前收集的物品類型
    public static Quaternion CurrentSpawnRotation { get; private set; } // 記錄當前收集的物品類型
    
    [Header("Visual Feedback")]
    [SerializeField] private LineRenderer lineRendererPrefab;
    private Dictionary<Rigidbody, LineRenderer> activeLines = new();

    
    private void Start()
    {
        CollectionSystem.LoadCollection(); // 遊戲開始時讀取收集數據
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2)) //Restart
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            ClearCollection();
        }
        if (Input.GetKeyDown(KeyCode.Escape)) //Close
        {
            Application.Quit();
        }
    }
    
    private void OnDisable()
    {
        foreach (var line in activeLines.Values)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        activeLines.Clear();
    }

    public void OnCollectCollectibles()
    {
        if (CollectionSystem.GetDictionaryCount() == 0) // 背包空間
        {
            FindCollectibles();
            MoveCollectibles();
        }
    }
    
    public void ClearCollection()
    {
        CollectionSystem.ClearCollection();
    }
    
    public float collectRadius = 1f; 
    public float collectAngle = 90f; 
    public float attractionSpeed = 3f; 
    public LayerMask collectibleLayer;
    public Transform collectPoint;

    private List<Rigidbody> attractedObjects = new List<Rigidbody>();

    private void FindCollectibles()
    {
        Collider[] collectibles = Physics.OverlapSphere(collectPoint.position, collectRadius, collectibleLayer);

        foreach (Collider collectible in collectibles)
        {
            if (IsInFront(collectible.transform)) 
            {
                SpawnObject spawnObj = collectible.GetComponent<SpawnObject>();
                if (spawnObj != null && spawnObj.isCollectable) // 檢查是否可被收集
                {
                    Rigidbody rb = collectible.GetComponent<Rigidbody>();
                    if (rb != null && !attractedObjects.Contains(rb))
                    {
                        rb.useGravity = false;
                        rb.linearDamping = 2f; 
                        attractedObjects.Add(rb);
                        
                        LineRenderer line = Instantiate(lineRendererPrefab);
                        line.positionCount = 2;
                        activeLines[rb] = line;
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
            rb.linearVelocity = direction * attractionSpeed;

            if (activeLines.ContainsKey(rb))
            {
                var line = activeLines[rb];
                line.SetPosition(0, rb.position);
                line.SetPosition(1, collectPoint.position);
            }
            
            if (Vector3.Distance(rb.position, collectPoint.position) < 0.5f)
            {
                SpawnType collectedType = rb.GetComponent<SpawnObject>().spawnType;
                CollectionSystem.CollectItem(collectedType,rb.transform.rotation);
                CurrentSpawnRotation = rb.transform.rotation;
                CurrentSpawnType = collectedType; // 記錄當前收集的 SpawnType
                Debug.Log($"收集了 {CurrentSpawnType}");
                
                Destroy(rb.gameObject);
                
                if (activeLines.ContainsKey(rb))
                {
                    Destroy(activeLines[rb].gameObject);
                    activeLines.Remove(rb);
                }
                
                attractedObjects.RemoveAt(i);
            }
        }
    }

    private bool IsInFront(Transform target)
    {
        Vector3 directionToTarget = (target.position - collectPoint.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle < collectAngle * 0.8f; 
    }
}
