using UnityEngine;

/// <summary>
/// 輕量漂浮視覺效果：讓物件在原地做上下(可選水平)的漂浮動畫。
/// 適合場景裝飾，不影響遊戲邏輯。
/// </summary>
[DisallowMultipleComponent]
public class SimpleFloat : MonoBehaviour
{
    [Header("Vertical Float")]
    [Tooltip("上下漂浮的幅度")]
    public float floatAmplitude = 0.25f;
    [Tooltip("上下漂浮的速度")]
    public float floatSpeed = 1.2f;

    [Header("Horizontal Sway (optional)")]
    [Tooltip("是否啟用水平(左右/前後)的擺動")]
    public bool enableSway = false;
    [Tooltip("水平擺動的幅度")]
    public float swayAmplitude = 0.1f;
    [Tooltip("水平擺動的速度")]
    public float swaySpeed = 0.8f;
    [Tooltip("擺動方向（世界座標）")]
    public Vector3 swayDirection = Vector3.right;

    [Header("Randomize")]
    [Tooltip("是否在開始時加入隨機位相，避免全部一起動")]
    public bool randomizePhase = true;

    private Vector3 _basePos;
    private float _phaseOffset;

    void Awake()
    {
        _basePos = transform.localPosition;
        _phaseOffset = randomizePhase ? Random.value * 10f : 0f;
        // 確保方向是單位向量，避免 inspector 填奇怪數字導致幅度意外變大
        if (enableSway && swayDirection.sqrMagnitude > 0.0001f)
            swayDirection = swayDirection.normalized;
    }

    void Update()
    {
        // 用 Time.time 直接跑，輕量、不用自己累加
        float t = Time.time + _phaseOffset;

        // 垂直位移
        float yOffset = Mathf.Sin(t * floatSpeed) * floatAmplitude;

        // 水平位移（可關）
        Vector3 sway = Vector3.zero;
        if (enableSway)
        {
            float swayOffset = Mathf.Sin(t * swaySpeed) * swayAmplitude;
            sway = swayDirection * swayOffset;
        }

        // 套用到 localPosition，讓你 prefab 放哪就飄哪
        transform.localPosition = _basePos + new Vector3(0f, yOffset, 0f) + sway;
    }

    // 給你一個在編輯器裡快速重設當前位置為基準的功能
#if UNITY_EDITOR
    [ContextMenu("Reset Base Position To Current")]
    private void ResetBasePosition()
    {
        _basePos = transform.localPosition;
    }
#endif
}