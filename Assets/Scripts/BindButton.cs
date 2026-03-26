using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BindButton : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private bool isExitButton = false;

    [Tooltip("要切換的場景名稱（須與 Build Settings 一致）")]
    [SerializeField] private string targetSceneName = "L1v6 qt0317"; // 預設值

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        TryBind();
    }

    private void TryBind()
    {
        var controller = SceneController.Instance;
        if (controller == null) return;

        // 移除所有舊監聽，避免重複綁定導致點一次觸發多次
        _button.onClick.RemoveAllListeners();

        if (isExitButton)
        {
            _button.onClick.AddListener(() => controller.ExitGame());
        }
        else
        {
            // 使用變數 targetSceneName 代替寫死的字串
            _button.onClick.AddListener(() => controller.LoadSceneWithLoading(targetSceneName));
        }
    }
}