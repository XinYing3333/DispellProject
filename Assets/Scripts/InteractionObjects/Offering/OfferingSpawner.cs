using System;
using DefaultNamespace.Thought;
using Player.InteractionSystem;
using UnityEngine;
using Random = UnityEngine.Random;

public class OfferingSpawner : MonoBehaviour ,IHitReceiver
{
    [Header("Spawn")]
    [SerializeField] private int spawn = 3;
    [SerializeField] private bool haveSpawnMax = true;
    [SerializeField] private int spawnMaxTime = 3;
    private int spawnCurrentTime = 0;
    
    [SerializeField] private int maxVisualPieces = 10; // 視覺上限
    [SerializeField] private float scatterRadius = 1.2f;
    [SerializeField] private Vector2 upImpulse = new Vector2(0.8f, 1.6f);

    [Header("Gizmos (Editor Only)")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color radiusColor = new Color(1f, 0.85f, 0.2f, 0.6f);
    [SerializeField] private Color edgeColor   = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField, Tooltip("預覽生成點（僅場景視圖顯示，無效於執行邏輯）")]
    private int previewPoints = 8;
    [SerializeField, Tooltip("預覽點的半徑（僅可視化）")]
    private float previewDotRadius = 0.06f;
    [SerializeField, Tooltip("固定預覽隨機種子，方便調整時觀察")]
    private int previewSeed = 12345;

    private void OnValidate()
    {
        scatterRadius = Mathf.Max(0f, scatterRadius);
        maxVisualPieces = Mathf.Max(1, maxVisualPieces);
        spawn = Mathf.Max(0, spawn);
        previewPoints = Mathf.Clamp(previewPoints, 0, 128);
        previewDotRadius = Mathf.Max(0.005f, previewDotRadius);
        if (upImpulse.x > upImpulse.y) (upImpulse.x, upImpulse.y) = (upImpulse.y, upImpulse.x);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            SpawnPieces(spawn);
    }

    
    // 以「總價值」生成，會自動聚合到有限顆數
    public void SpawnTotalValue(int totalValue, CollectionSystem.CollectedType type = CollectionSystem.CollectedType.Though)
    {
        if (totalValue <= 0) return;

        int pieceCount = Mathf.Min(maxVisualPieces, totalValue);
        int baseVal = totalValue / pieceCount;
        int remainder = totalValue % pieceCount;

        for (int i = 0; i < pieceCount; i++)
        {
            var p = OfferingPool.Instance.Get();
            p.type = type;
            p.value = baseVal + (i < remainder ? 1 : 0);

            Vector3 pos = transform.position + (Vector3)Random.insideUnitCircle * scatterRadius;
            pos.y += Random.Range(upImpulse.x, upImpulse.y);
            p.transform.position = pos;
        }
    }

    // 舊版等量生成（只在數量小時用）
    public void SpawnPieces(int amount, int valuePerPiece = 1, CollectionSystem.CollectedType type = CollectionSystem.CollectedType.Though)
    {
        if(spawnCurrentTime >= spawnMaxTime)return;
        spawnCurrentTime += 1;
        SpawnTotalValue(amount * valuePerPiece, type);
    }

    // ---------- Gizmos 可視化 ----------
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // 填色圓盤（半透明）
        Gizmos.color = radiusColor;
        DrawSolidDiscXZ(transform.position, scatterRadius, 0.1f);

#if UNITY_EDITOR
        // 邊線（Handles畫更清晰的圓）
        UnityEditor.Handles.color = edgeColor;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, scatterRadius);
#else
        // 退回：用一個薄的球圈表示
        Gizmos.color = edgeColor;
        Gizmos.DrawWireSphere(transform.position, scatterRadius);
#endif

        // 預覽幾個生成點
        if (previewPoints > 0 && scatterRadius > 0f)
        {
            var old = Random.state;
            Random.InitState(previewSeed);

            for (int i = 0; i < previewPoints; i++)
            {
                Vector2 r2 = Random.insideUnitCircle * scatterRadius;
                float y = Mathf.Lerp(upImpulse.x, upImpulse.y, Random.value);
                Vector3 p = transform.position + new Vector3(r2.x, y, r2.y);

                DrawSolidDiscXZ(p, previewDotRadius, 0.5f);
            }

            Random.state = old;
        }
    }

    /// <summary>
    /// 在 XZ 平面畫一個實心圓盤（用多個小三角扇模擬；Gizmos沒有直接的Disc）
    /// </summary>
    private void DrawSolidDiscXZ(Vector3 center, float radius, float yOffset = 0f, int segments = 32)
    {
        if (radius <= 0f) return;

        Vector3 up = Vector3.up;
        Vector3 right = Vector3.right;
        Vector3 forward = Vector3.forward;

        Vector3 c = center + up * yOffset;
        float step = Mathf.PI * 2f / segments;

        // 用三角扇畫近似填充
        for (int i = 0; i < segments; i++)
        {
            float a0 = step * i;
            float a1 = step * (i + 1);

            Vector3 p0 = c + (right * Mathf.Cos(a0) + forward * Mathf.Sin(a0)) * radius;
            Vector3 p1 = c + (right * Mathf.Cos(a1) + forward * Mathf.Sin(a1)) * radius;

            Gizmos.DrawLine(c, p0);
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, c);
        }
    }

    public void OnHit(ThoughtPayloadSO payload)
    {
        SpawnPieces(spawn);
    }

    public void OnFocusGained()
    {
        throw new NotImplementedException();
    }

    public void OnFocusLost()
    {
        throw new NotImplementedException();
    }
}
