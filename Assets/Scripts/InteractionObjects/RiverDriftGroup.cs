using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 控制本物件底下所有子物件在路徑上漂流，帶有隨機浮動、旋轉與偏移。
/// 自動讀取子物件，不用手動設定清單。
/// 在 Scene 視圖會顯示路徑與 maxSideOffset 可視化範圍。
/// </summary>
public class RiverDriftGroupAuto : MonoBehaviour
{
    [Header("路徑設定")]
    public Transform startPoint;
    public Transform endPoint;
    public float driftSpeed = 1f;

    [Header("隨機起始")]
    public bool randomizeStartT = true;

    [Header("水平偏移設定")]
    [Tooltip("漂流物會在路徑兩側最大偏移距離內隨機散開（世界座標）")]
    public float maxSideOffset = 0.4f;
    public bool usePathRightAsSide = true;

    [Header("浮動設定（隨機範圍）")]
    public float floatAmplitudeMin = 0.1f;
    public float floatAmplitudeMax = 0.3f;
    public float floatFrequencyMin = 0.5f;
    public float floatFrequencyMax = 1.5f;

    [Header("旋轉設定（隨機範圍）")]
    public float rotationSpeedMin = -15f;
    public float rotationSpeedMax = 15f;

    [Header("速度微亂數")]
    [Range(0f, 1f)]
    [Tooltip("每個 item 的速度 = driftSpeed * Random.Range(1 - x , 1 + x)")]
    public float perItemSpeedJitter = 0.15f;

    // --- 內部結構 ---
    private struct ItemData
    {
        public Transform tr;
        public float t;
        public Vector3 sideOffset;
        public float floatAmp;
        public float floatFreq;
        public float rotSpeed;
        public float speedFactor;
        public float phase;
    }

    private List<ItemData> _items = new List<ItemData>();
    private Vector3 _startPos, _endPos, _pathDir, _sideDir;
    private float _pathLength;

    private void Awake()
    {
        // 1️⃣ 設定路徑
        _startPos = startPoint ? startPoint.position : transform.position;
        _endPos   = endPoint   ? endPoint.position   : _startPos + Vector3.forward * 5f;

        Vector3 pathVec = _endPos - _startPos;
        _pathLength = pathVec.magnitude;
        _pathDir = (_pathLength > 0.0001f) ? pathVec / _pathLength : Vector3.forward;

        // 2️⃣ 左右方向
        if (usePathRightAsSide && _pathLength > 0.0001f)
        {
            _sideDir = Vector3.Cross(Vector3.up, _pathDir).normalized;
            if (_sideDir.sqrMagnitude < 1e-4f)
                _sideDir = Vector3.right;
        }
        else
        {
            _sideDir = Vector3.right;
        }

        // 3️⃣ 收集子物件
        _items.Clear();
        foreach (Transform child in transform)
        {
            var d = new ItemData();
            d.tr = child;

            d.t = randomizeStartT ? Random.value : 0f;
            float side = (maxSideOffset > 0f) ? Random.Range(-maxSideOffset, maxSideOffset) : 0f;
            d.sideOffset = _sideDir * side;

            d.floatAmp = Random.Range(floatAmplitudeMin, floatAmplitudeMax);
            d.floatFreq = Random.Range(floatFrequencyMin, floatFrequencyMax);
            d.phase = Random.Range(0f, 10f);
            d.rotSpeed = Random.Range(rotationSpeedMin, rotationSpeedMax);

            if (perItemSpeedJitter > 0f)
            {
                float j = perItemSpeedJitter;
                d.speedFactor = Random.Range(1f - j, 1f + j);
            }
            else d.speedFactor = 1f;

            _items.Add(d);
        }
    }

    private void Update()
    {
        if (_pathLength < 0.001f || _items.Count == 0) return;

        float time = Time.time;
        float baseDeltaT = (driftSpeed / _pathLength) * Time.deltaTime;

        for (int i = 0; i < _items.Count; i++)
        {
            var d = _items[i];
            if (!d.tr) continue;

            d.t += baseDeltaT * d.speedFactor;
            if (d.t >= 1f) d.t = 0f;

            Vector3 pos = Vector3.Lerp(_startPos, _endPos, d.t);
            pos += d.sideOffset;

            if (d.floatAmp > 0f)
            {
                float floatOffset = Mathf.Sin((time + d.phase) * d.floatFreq) * d.floatAmp;
                pos.y += floatOffset;
            }

            d.tr.position = pos;

            if (Mathf.Abs(d.rotSpeed) > 0.01f)
                d.tr.Rotate(Vector3.up, d.rotSpeed * Time.deltaTime, Space.World);

            _items[i] = d;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 更新可視化資料
        Vector3 s = startPoint ? startPoint.position : transform.position;
        Vector3 e = endPoint   ? endPoint.position   : s + Vector3.forward * 5f;
        Vector3 path = e - s;
        float len = path.magnitude;
        Vector3 dir = (len > 0.001f) ? path / len : Vector3.forward;
        Vector3 side = (usePathRightAsSide && len > 0.001f)
            ? Vector3.Cross(Vector3.up, dir).normalized
            : Vector3.right;

        // 主路徑
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(s, e);
        Gizmos.DrawSphere(s, 0.12f);
        Gizmos.DrawSphere(e, 0.12f);

        // 可視化 maxSideOffset 區域
        if (maxSideOffset > 0f)
        {
            Vector3 sLeft = s - side * maxSideOffset;
            Vector3 sRight = s + side * maxSideOffset;
            Vector3 eLeft = e - side * maxSideOffset;
            Vector3 eRight = e + side * maxSideOffset;

            Color fillColor = new Color(0f, 1f, 1f, 0.15f);
            Gizmos.color = fillColor;

            // 半透明面（只有 Scene 模式才畫）
            Gizmos.DrawLine(sLeft, eLeft);
            Gizmos.DrawLine(sRight, eRight);
            Gizmos.DrawLine(sLeft, sRight);
            Gizmos.DrawLine(eLeft, eRight);

#if UNITY_EDITOR
            // 填充中間區域（只在 SceneView 顯示）
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            UnityEditor.Handles.color = fillColor;
            UnityEditor.Handles.DrawAAConvexPolygon(sLeft, sRight, eRight, eLeft);
#endif
        }
    }
#endif
}
