using UnityEngine;

public class CCAnimCtrl : MonoBehaviour
{
    private Animator animator;

    [SerializeField] string parameterName = "still";
    
    // 當你在 Inspector 拖動或修改這個值時，動畫會立即反應
    [Range(0, 5)]
    [SerializeField] int value = 1;

    private int parameterHash;

    void Awake()
    {
        Initialize();
    }

    void Start()
    {
        ApplyValue();
    }

    // 當 Inspector 中的數值被改變時（或是腳本剛加載時）會被調用
    private void OnValidate()
    {
        // 確保在編輯模式下也能抓到組件並更新
        Initialize();
        ApplyValue();
    }

    private void Initialize()
    {
        if (animator == null) animator = GetComponent<Animator>();
        parameterHash = Animator.StringToHash(parameterName);
    }

    public void ApplyValue()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetInteger(parameterHash, value);
        }
    }
}