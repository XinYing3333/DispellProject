using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class ThoughtPlacer : MonoBehaviour
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
    
    [Header("曲線控制點（至少 2 個）")]
    public List<Transform> curvePoints = new List<Transform>();

    [Header("曲線取樣精度（越大越平滑）")]
    [Range(8, 200)] public int curveResolution = 40;


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

            GameObject obj = ThoughtPoolManager.Instance.Get(thoughPositions[i]);
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

        ThoughtPoolManager.Instance.Return(obj);
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

        // 1) 組出控制點：優先用 curvePoints；若沒填就退回 start/end
        var pts = BuildCurvePointPositions();
        if (pts.Count < 2) return;

        // 2) 先高解析度取樣曲線成 polyline
        var poly = SampleCatmullRomPolyline(pts, curveResolution);

        // 3) 依 spacing 做等距取樣，產生擺放點
        thoughPositions = ResampleBySpacing(poly, spacing, count);

        // 4) 建立穩定 id（沿用你原本格式）
        string scene = SceneManager.GetActiveScene().name;
        for (int i = 0; i < thoughPositions.Count; i++)
        {
            string id = $"{scene}:{placerId}:{i}";
            spawnIds.Add(id);
        }
    }
    
    private List<Vector3> BuildCurvePointPositions()
{
    var pts = new List<Vector3>();

    // curvePoints 有效就用它
    if (curvePoints != null)
    {
        for (int i = 0; i < curvePoints.Count; i++)
        {
            if (curvePoints[i] != null)
                pts.Add(curvePoints[i].position);
        }
    }

    // 若不足，退回 start/end
    if (pts.Count < 2)
    {
        if (startPoint != null) pts.Add(startPoint.position);
        if (endPoint != null) pts.Add(endPoint.position);
    }

    return pts;
}

// Catmull-Rom：可穿越控制點，適合關卡擺放路徑
private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
{
    float t2 = t * t;
    float t3 = t2 * t;

    return 0.5f * (
        (2f * p1) +
        (-p0 + p2) * t +
        (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
        (-p0 + 3f * p1 - 3f * p2 + p3) * t3
    );
}

// 把控制點曲線取樣成折線（polyline）
private static List<Vector3> SampleCatmullRomPolyline(List<Vector3> controlPoints, int resolutionPerSegment)
{
    var result = new List<Vector3>();
    if (controlPoints == null || controlPoints.Count < 2) return result;

    // 讓端點也能順
    Vector3 Get(int idx)
    {
        if (idx < 0) return controlPoints[0];
        if (idx >= controlPoints.Count) return controlPoints[controlPoints.Count - 1];
        return controlPoints[idx];
    }

    for (int i = 0; i < controlPoints.Count - 1; i++)
    {
        Vector3 p0 = Get(i - 1);
        Vector3 p1 = Get(i);
        Vector3 p2 = Get(i + 1);
        Vector3 p3 = Get(i + 2);

        // 每段取樣（包含起點，不重複加入段落交界點）
        int steps = Mathf.Max(2, resolutionPerSegment);
        for (int s = 0; s < steps; s++)
        {
            float t = s / (float)(steps - 1);
            Vector3 pos = CatmullRom(p0, p1, p2, p3, t);

            if (result.Count == 0 || (result[result.Count - 1] - pos).sqrMagnitude > 1e-8f)
                result.Add(pos);
        }
    }

    return result;
}

// 折線等距重取樣：真正保證 spacing
private static List<Vector3> ResampleBySpacing(List<Vector3> polyline, float spacing, int maxCount)
{
    var outPts = new List<Vector3>();
    if (polyline == null || polyline.Count == 0) return outPts;

    spacing = Mathf.Max(0.001f, spacing);
    maxCount = Mathf.Clamp(maxCount, 2, 10000);

    outPts.Add(polyline[0]);

    float accumulated = 0f;
    int seg = 0;

    while (outPts.Count < maxCount && seg < polyline.Count - 1)
    {
        Vector3 a = polyline[seg];
        Vector3 b = polyline[seg + 1];
        float segLen = Vector3.Distance(a, b);

        if (segLen <= 1e-6f)
        {
            seg++;
            continue;
        }

        float remaining = spacing - accumulated;

        if (segLen >= remaining)
        {
            float t = remaining / segLen;
            Vector3 newPoint = Vector3.Lerp(a, b, t);

            outPts.Add(newPoint);

            // 從新點接著走
            polyline[seg] = newPoint;
            accumulated = 0f;
        }
        else
        {
            accumulated += segLen;
            seg++;
        }
    }

    return outPts;
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
        if (!showPreview) return;

        var pts = BuildCurvePointPositions();
        if (pts.Count < 2) return;

        // 控制點
        Gizmos.color = Color.yellow;
        for (int i = 0; i < pts.Count; i++)
            Gizmos.DrawWireSphere(pts[i], 0.15f);

        // 曲線折線
        var poly = SampleCatmullRomPolyline(pts, curveResolution);
        Gizmos.color = Color.green;
        for (int i = 0; i < poly.Count - 1; i++)
            Gizmos.DrawLine(poly[i], poly[i + 1]);

        // 預擺放點（依 spacing / count）
        var placePts = ResampleBySpacing(poly, spacing, count);
        Gizmos.color = Color.cyan;
        for (int i = 0; i < placePts.Count; i++)
            Gizmos.DrawWireSphere(placePts[i], 0.2f);
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
                    ThoughtPoolManager.Instance.Return(obj);
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
