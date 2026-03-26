using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class SplineSwarmManager : MonoBehaviour
{
    [Header("基礎配置")]
    public SplineContainer splineContainer;
    public GameObject antPrefab;
    [Range(1, 50)] public int count = 10;
    public float baseSpeed = 2.0f;
    public float spacing = 5f;

    [Header("隨機擾動 (Randomness)")]
    public float speedVariation = 0.5f;    // 速度隨機增減範圍
    public float sideOffsetRange = 0.2f;   // 左右偏移範圍（離散感）
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f); // 大小隨機化

    private List<Transform> instances = new List<Transform>();
    private float[] individualSpeeds;
    private float[] sideOffsets;
    private float totalLength;
    private float globalProgress = 0f;

    void Start()
    {
        if (splineContainer == null) return;
        
        totalLength = splineContainer.CalculateLength();
        individualSpeeds = new float[count];
        sideOffsets = new float[count];
        
        SpawnSwarm();
    }

    void SpawnSwarm()
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(antPrefab, transform);
            instances.Add(go.transform);

            // 初始化隨機數據
            individualSpeeds[i] = baseSpeed + Random.Range(-speedVariation, speedVariation);
            sideOffsets[i] = Random.Range(-sideOffsetRange, sideOffsetRange);
            go.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
        }
    }

    void Update()
    {
        if (splineContainer == null || instances.Count == 0) return;

        for (int i = 0; i < instances.Count; i++)
        {
            // 1. 計算每隻獨立的進度 (基於各自的速度與初始間距)
            float distanceOnSpline = (i * spacing) + (individualSpeeds[i] * Time.time);
            float t = (distanceOnSpline / totalLength) % 1f;

            // 2. 取得基礎位置與前進方向
            Vector3 pos = (Vector3)splineContainer.EvaluatePosition(t);
            Vector3 forward = (Vector3)splineContainer.EvaluateTangent(t);
            Vector3 up = (Vector3)splineContainer.EvaluateUpVector(t);

            // 3. 計算左右偏移 (使用外積取得側邊向量)
            Vector3 sideDir = Vector3.Cross(up, forward).normalized;
            pos += sideDir * sideOffsets[i];

            // 4. 套用變換
            instances[i].position = pos;
            if (forward.sqrMagnitude > 0.001f)
            {
                instances[i].rotation = Quaternion.LookRotation(forward, up);
            }
        }
    }
}