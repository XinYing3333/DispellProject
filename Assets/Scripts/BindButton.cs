using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 掛在 Button 上，自動尋找 SceneController 並綁定指定方法。
/// </summary>
[RequireComponent(typeof(Button))]
public class BindButton : MonoBehaviour
{
    [Tooltip("要呼叫 SceneController 的哪個 public 方法（方法必須無參數）")]

    [SerializeField] private bool isExitButton = false;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        //_button.onClick.RemoveAllListeners(); // 清空防重綁
    }

    private void OnEnable()
    {
        TryBind();
    }

    private void OnSceneLoaded()
    {
        // 如果場景切換後還活著，可以再重新綁
        TryBind();
    }

    private void TryBind()
    {
        var controller = SceneController.Instance;
        if (controller == null)
        {
            Debug.LogWarning($"[BindToSceneController] 找不到 SceneController，等待中… ({name})");
            return;
        }

        if (isExitButton)
        {
            _button.onClick.AddListener(() => controller.ExitGame());
            return;
        }
        
        // TODO:切場景不要寫在程式裏
        // 綁定時用 lambda 呼叫該方法
        _button.onClick.AddListener(() => controller.LoadSceneWithLoading("L1v5 qt"));
    }
}