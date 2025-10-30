using UnityEngine;

/// <summary>
/// 踩下去會往下壓，再慢慢回彈到原本位置的簡易效果。
/// 沒有用物理、沒有 coroutine，只有一點 Lerp，很輕量。
/// </summary>
public class PressDownAndReturn : MonoBehaviour
{
    [Header("壓下設定")]
    public float pressDepth = 0.15f;   // 壓多深（世界座標往下）
    public float pressSpeed = 20f;     // 壓下的速度（越大越快到位）

    [Header("回彈設定")]
    public float returnSpeed = 5f;     // 回彈速度（越大越快回原位）
    public bool overshoot = false;     // 要不要一點點超過再回來
    public float overshootAmount = 0.03f;

    private Vector3 _restPos;          // 原本位置
    private Vector3 _targetPos;        // 當前要去的位置
    private bool _isPressed;           // 正在處於「被壓」那瞬間
    private float _timeSincePress;

    void Awake()
    {
        _restPos = transform.position;
        _targetPos = _restPos;
    }

    void Update()
    {
        // 每幀往 target 移一點
        transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * (_isPressed ? pressSpeed : returnSpeed));

        // 壓完要開始回去
        if (_isPressed)
        {
            _timeSincePress += Time.deltaTime;
            if (_timeSincePress >= 0.12f)   // 壓著的時間，超過就回彈
            {
                _isPressed = false;
                // 回到原位，若要一點點彈性就多加一點
                _targetPos = _restPos + (overshoot ? Vector3.up * overshootAmount : Vector3.zero);
            }
        }
        else
        {
            // 回到原位後，還原 target
            if (Vector3.SqrMagnitude(transform.position - _restPos) < 0.0001f)
            {
                _targetPos = _restPos;
            }
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Press();
        }
    }


    /// <summary>
    /// 外部呼叫這個，平台就會壓下去一次
    /// </summary>
    public void Press()
    {
        _isPressed = true;
        _timeSincePress = 0f;
        _targetPos = _restPos + Vector3.down * pressDepth;
    }

    /// <summary>
    /// 如果你想要移動整個物件到別處，可以再重設原點
    /// </summary>
    public void ResetRestPosition()
    {
        _restPos = transform.position;
        _targetPos = _restPos;
    }
}