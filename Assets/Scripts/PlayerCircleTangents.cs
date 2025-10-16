using UnityEngine;

#if UNITY_EDITOR
using UnityEditor; // 只為了在 Scene 標註文字
#endif

/// <summary>
/// 掛在「玩家」身上：求「Target→Player 的直線」與
/// 以玩家為圓心、半徑 radius 的圓在 XZ 平面上的兩個交點：PointC(目標側) 與 PointB(另一側)。
/// 也會用 Gizmos 畫示意圖：圓、紅線、兩個紅點。
/// </summary>
[ExecuteAlways]
public class PlayerCircleTangents : MonoBehaviour
{
    [Header("Setup")]
    public Transform target;              // Target（外部點）
    [Min(0f)] public float radius = 2f;  // 玩家周圍的圓半徑（XZ 平面）
    [Tooltip("紅線在兩端額外延伸的長度（只影響 Gizmos 視覺）")]
    public float lineExtend = 4f;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color circleColor = new Color(1f, 0.85f, 0f, 0.9f);
    public Color lineColor   = new Color(1f, 0.25f, 0.1f, 1f);
    public Color pointColor  = new Color(1f, 0.2f, 0.1f, 1f);
    public float pointSize   = 0.12f;

    // 計算結果（世界座標）
    public Vector3 pointC { get; private set; } // 目標側（進入）交點
    public Vector3 pointB { get; private set; } // 另一側（離開）交點
    public bool hasIntersections { get; private set; }

    void Update()
    {
        if (!Application.isPlaying) Compute();
    }

    void LateUpdate()
    {
        if (Application.isPlaying) Compute();
    }

    /// <summary>
    /// 計算：直線 (target→player) 與 以 player 為圓心、半徑 r 的圓在 XZ 平面上的兩個交點。
    /// 若直線與圓無交點（目標與玩家重合或半徑太小等），hasIntersections=false。
    /// </summary>
    void Compute()
    {
        hasIntersections = false;
        if (target == null || radius <= 0f) return;

        // ---- XZ 平面座標（忽略高度）----
        Vector3 C3 = transform.position;
        Vector3 P3 = target.position;

        Vector2 C = new Vector2(C3.x, C3.z);
        Vector2 P0 = new Vector2(P3.x, P3.z);
        Vector2 P1 = new Vector2(C3.x, C3.z); // 直線方向使用 target→player
        Vector2 d = (P1 - P0);
        float dLen2 = d.sqrMagnitude;

        if (dLen2 < 1e-10f) return; // target 與 player 重合，無法定義直線

        // 令 L(t) = P0 + t * d，t ∈ (-∞, +∞)
        // 解 |L(t) - C|^2 = r^2 之二次式
        float r = radius;
        float a = dLen2;
        Vector2 f = P0 - C;
        float b = 2f * Vector2.Dot(d, f);
        float c = Vector2.Dot(f, f) - r * r;

        float disc = b * b - 4f * a * c;
        if (disc < 0f) return; // 無交點

        float sqrtD = Mathf.Sqrt(Mathf.Max(0f, disc));
        float t1 = (-b - sqrtD) / (2f * a);
        float t2 = (-b + sqrtD) / (2f * a);

        // 兩個交點（未排序）
        Vector2 I1 = P0 + t1 * d;
        Vector2 I2 = P0 + t2 * d;

        // 依「由 target 指向 player 的方向」決定先後：
        // 先遇到的是「目標側」交點（PointC），另一個是 PointB。
        // 這裡把「較小的 t」視為先遇點（因為我們的線定義為 P0->P1）。
        Vector2 Cxz, Bxz;
        if (t1 < t2) { Cxz = I1; Bxz = I2; }
        else         { Cxz = I2; Bxz = I1; }

        // 回到世界座標（取玩家/目標的高度中線，讓點落在地面高度）
        float y = C3.y; // 或取地面高度，依需求調整
        pointC = new Vector3(Cxz.x, y, Cxz.y);
        pointB = new Vector3(Bxz.x, y, Bxz.y);
        hasIntersections = true;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // 畫玩家圓（以 DrawWireSphere 近似 XZ 圓）
        Gizmos.color = circleColor;
        Gizmos.DrawWireSphere(transform.position, radius);

        if (target != null)
        {
            // 畫紅線（target→player 的延伸線）
            Vector3 C3 = transform.position;
            Vector3 P3 = target.position;
            Vector3 dir = (C3 - P3).normalized;
            if (dir.sqrMagnitude > 1e-8f)
            {
                Vector3 pStart = P3 - dir * lineExtend;
                Vector3 pEnd   = C3 + dir * (radius + lineExtend);
                Gizmos.color = lineColor;
                Gizmos.DrawLine(pStart, pEnd);
            }

            // 畫兩個交點
            if (hasIntersections)
            {
                Gizmos.color = pointColor;
                Gizmos.DrawSphere(pointC, pointSize); // 目標側交點（示意圖的 Point C）
                Gizmos.DrawSphere(pointB, pointSize); // 另一側交點（示意圖的 Point B）

                // 也把交點到目標的線段畫一下（更像你的示意圖）
                Gizmos.DrawLine(target.position, pointC);
                Gizmos.DrawLine(pointC, pointB);
            }

#if UNITY_EDITOR
            // 在 Scene 檢視標註文字
            Handles.color = Color.white;
            if (hasIntersections)
            {
                Handles.Label(pointC + Vector3.up * 0.05f, "Point C");
                Handles.Label(pointB + Vector3.up * 0.05f, "Point B");
            }
            Handles.Label(transform.position + Vector3.up * 0.05f, "Player");
            Handles.Label(target.position   + Vector3.up * 0.05f, "Target");
#endif
        }
    }
}
