using UnityEngine;

public class DriftAlongRiver : MonoBehaviour
{
    [Header("漂流設定")]
    public Vector3 driftDirection = Vector3.forward; // 漂流方向
    public float driftSpeed = 1f;

    [Header("浮動設定")]
    public float floatAmplitude = 0.2f; // 浮動高度
    public float floatFrequency = 1f;   // 浮動頻率

    [Header("旋轉設定")]
    public float rotationSpeed = 10f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        driftDirection.Normalize();
    }

    void Update()
    {
        // 漂流移動
        transform.position += driftDirection * driftSpeed * Time.deltaTime;

        // 浮動
        float floatOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        Vector3 pos = transform.position;
        pos.y = startPos.y + floatOffset;
        transform.position = pos;

        // 微微旋轉
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        // 畫一個箭頭表示漂流方向
        Vector3 start = transform.position;
        Vector3 end = start + driftDirection.normalized * 2f;

        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);
    }
#endif

}