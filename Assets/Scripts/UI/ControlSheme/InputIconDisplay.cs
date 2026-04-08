using UnityEngine;
using UnityEngine.UI;
using DefaultNamespace.ControlSheme;

public class InputIconDisplay : MonoBehaviour
{
    [SerializeField] private ActionName actionName;
    [SerializeField] private Image iconImage;
    [SerializeField] private InputBindingLibrary bindingLibrary;

    private void OnEnable()
    {
        if (ControlSchemeHint.Instance != null)
        {
            ControlSchemeHint.Instance.OnModeChanged += UpdateIcon;
            // 初始顯示
            UpdateIcon(ControlSchemeHint.Instance.CurrentMode);
        }
    }

    private void OnDisable()
    {
        if (ControlSchemeHint.Instance != null)
            ControlSchemeHint.Instance.OnModeChanged -= UpdateIcon;
    }

    // 提供外部動態更換 Action 的能力
    public void SetAction(ActionName newAction)
    {
        actionName = newAction;
        if (ControlSchemeHint.Instance != null)
            UpdateIcon(ControlSchemeHint.Instance.CurrentMode);
    }

    private void UpdateIcon(ControlSchemeHint.UIInputMode mode)
    {
        bool isGamepad = (mode == ControlSchemeHint.UIInputMode.Gamepad);
        Sprite s = bindingLibrary.GetSprite(actionName, isGamepad);
        if (s != null && iconImage != null)
        {
            iconImage.sprite = s;
        }
    }
}