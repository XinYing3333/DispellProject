using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverOrSelectInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject infoPanel;

    private bool isHovered = false;
    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        infoPanel.SetActive(false);
    }

    private void Update()
    {
        InputDetector.UpdateInputMode();

        bool show =
            (InputDetector.CurrentInputMode == InputMode.Gamepad && EventSystem.current.currentSelectedGameObject == gameObject) ||
            (InputDetector.CurrentInputMode == InputMode.Mouse && isHovered);

        infoPanel.SetActive(show);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}