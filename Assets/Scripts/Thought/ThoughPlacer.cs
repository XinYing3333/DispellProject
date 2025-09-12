using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class ThoughPlacer : MonoBehaviour
{
    [Header("Prefab 設定")]
    public GameObject thoughPrefab;

    [Header("排列參數")]
    [Range(2, 100)] public int count = 10;
    public float spacing = 1.5f;
    public float arcHeight = 1f;

    [Header("擺放起點與終點")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("可視化設定")]
    public bool showPreview = true;
    public bool generateThough = false;
    public bool clearThough = false;

    private List<Vector3> thoughPositions = new List<Vector3>();
    private List<GameObject> activeThough = new List<GameObject>();
    private bool hasInitialized = false;
    // 放在類別欄位處
    [SerializeField, HideInInspector] private string placerId;
    private List<string> spawnIds = new List<string>();


    private void Start()
    {
        if (Application.isPlaying)
        {
            hasInitialized = false;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (generateThough)
            {
                GenerateThoughForPreview();
                generateThough = false;
            }

            if (clearThough)
            {
                ClearExisting();
                clearThough = false;
            }
        }
#endif

        if (Application.isPlaying && !hasInitialized)
        {
            CalculatePositions();
            ActivateThoughFromSharedPool();
            hasInitialized = true;
        }
    }

    public void Refresh()
    {
        ClearExisting();
        CalculatePositions();
        ActivateThoughFromSharedPool();
    }
    
    public void ActivateThoughFromSharedPool()
    {
        if (!Application.isPlaying || thoughPrefab == null) return;

        activeThough.Clear();

        for (int i = 0; i < thoughPositions.Count; i++)
        {
            string id = spawnIds[i];
            if (LevelStateStore.Instance != null && LevelStateStore.Instance.IsCollectedNow(id))
                continue;

            GameObject obj = ThoughPoolManager.Instance.Get(thoughPositions[i]);
            if (obj != null)
            {
                obj.transform.SetParent(transform);
                var collectible = obj.GetComponent<ThoughtCollectible>();
                if (collectible != null) collectible.Init(id, this);
                activeThough.Add(obj);
            }
        }
    }



    public void ReturnThoughToPool(GameObject obj)
    {
        if (obj == null) return;

        ThoughPoolManager.Instance.Return(obj);
        activeThough.Remove(obj);
    }

    public void GenerateThoughForPreview()
    {
        if (thoughPrefab == null || startPoint == null || endPoint == null)
        {
            Debug.LogWarning("請指定 prefab 與起點/終點");
            return;
        }

        ClearExisting();
        CalculatePositions();

#if UNITY_EDITOR
        for (int i = 0; i < thoughPositions.Count; i++)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(thoughPrefab);
            obj.transform.position = thoughPositions[i];
            obj.transform.SetParent(transform);
            Undo.RegisterCreatedObjectUndo(obj, "Spawn Though");
        }
#endif
    }

    private void CalculatePositions()
    {
        thoughPositions.Clear();
        spawnIds.Clear();

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            thoughPositions.Add(GetArcPosition(t));

            string scene = SceneManager.GetActiveScene().name;
            string id = $"{scene}:{placerId}:{i}";
            spawnIds.Add(id);
        }
    }


    private Vector3 GetArcPosition(float t)
    {
        if (startPoint == null || endPoint == null) return transform.position;

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;
        Vector3 mid = Vector3.Lerp(start, end, t);
        float heightOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;
        return mid + Vector3.up * heightOffset;
    }

    private void OnDrawGizmos()
    {
        if (!showPreview || startPoint == null || endPoint == null) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector3 pos = GetArcPosition(t);
            Gizmos.DrawWireSphere(pos, 0.2f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startPoint.position, endPoint.position);
    }

    private void ClearExisting()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
        else
#endif
        {
            foreach (var obj in activeThough)
            {
                if (obj != null)
                    ThoughPoolManager.Instance.Return(obj);
            }

            activeThough.Clear();
        }
    }   
    // 在編輯器下只生成一次穩定的 placerId
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(placerId))
        {
            placerId = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif

}
