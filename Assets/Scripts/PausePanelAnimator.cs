using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class PausePanelAnimator : MonoBehaviour
{
    [Header("主要面板")] public RectTransform pausePanel;

    [Header("Menu 按鈕 & 面板")] public RectTransform menuButton;
    public RectTransform menuPanel;

    [Header("動畫設定")] public float animationDuration = 0.3f;
    public float menuButtonMoveY = 100f;
    public float menuPanelTargetScale = 0.35f;

    [Header("Menu Button Text")] public TextMeshProUGUI menuButtonText;

    [Header("PausePanel 偏移設定 (打開Menu時)")] public Vector3 pausePanelMenuOffset = new Vector3(-50f, 0f, 0f);
    public float pausePanelMenuRotateZ = 10f;

    [Header("MenuPanel 子物件")] public Button yesButton;
    public Button noButton;

    [Header("按鈕文字 hover 效果")] public float hoverScale = 1.1f;
    public Vector3 hoverOffset = new Vector3(5f, 5f, 0f);
    public Color hoverColor = Color.yellow;

    [Header("逐字掉落設定")] public float dropDistance = 50f;
    public float dropDuration = 0.5f;
    public float dropDelayBetween = 0.05f;

    private bool isPauseOpen = false;
    private bool isMenuOpen = false;

    private float menuButtonInitialLocalY;
    private Vector3 pausePanelInitialPos;
    private Vector3 pausePanelInitialRot;

    // Yes / No 按鈕文字
    private TextMeshProUGUI yesText;
    private TextMeshProUGUI noText;
    private Vector3 yesTextInitialPos, noTextInitialPos;
    private Vector3 yesTextInitialScale, noTextInitialScale;
    private Color yesTextInitialColor, noTextInitialColor;

    void Start()
    {
        // 記錄 PausePanel 初始位置 & 旋轉
        if (pausePanel != null)
        {
            pausePanelInitialPos = pausePanel.localPosition;
            pausePanelInitialRot = pausePanel.localEulerAngles;
            pausePanel.localScale = Vector3.zero;
            pausePanel.localEulerAngles = new Vector3(0, 0, -90);
            pausePanel.gameObject.SetActive(false);
        }

        if (menuButton != null)
        {
            menuButtonInitialLocalY = menuButton.localPosition.y;
            menuButton.gameObject.SetActive(false);
        }

        if (menuPanel != null)
        {
            menuPanel.localScale = Vector3.zero;
            menuPanel.gameObject.SetActive(false);
        }

        // 初始化 Yes / No 按鈕文字
        if (yesButton != null)
        {
            yesText = yesButton.GetComponentInChildren<TextMeshProUGUI>();
            yesTextInitialPos = yesText.rectTransform.localPosition;
            yesTextInitialScale = yesText.rectTransform.localScale;
            yesTextInitialColor = yesText.color;

            yesButton.onClick.AddListener(() => CloseMenuPanel());
            AddHoverEvent(yesButton, yesText);
        }

        if (noButton != null)
        {
            noText = noButton.GetComponentInChildren<TextMeshProUGUI>();
            noTextInitialPos = noText.rectTransform.localPosition;
            noTextInitialScale = noText.rectTransform.localScale;
            noTextInitialColor = noText.color;

            noButton.onClick.AddListener(() => CloseMenuPanel());
            AddHoverEvent(noButton, noText);
        }
    }

    private void AddHoverEvent(Button btn, TextMeshProUGUI btnText)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((data) => OnButtonHover(btnText, true));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((data) => OnButtonHover(btnText, false));
        trigger.triggers.Add(exit);
    }

    private void OnButtonHover(TextMeshProUGUI btnText, bool enter)
    {
        if (btnText == null) return;

        Vector3 initialPos = (btnText == yesText) ? yesTextInitialPos : noTextInitialPos;
        Vector3 initialScale = (btnText == yesText) ? yesTextInitialScale : noTextInitialScale;
        Color initialColor = (btnText == yesText) ? yesTextInitialColor : noTextInitialColor;
        RectTransform rect = btnText.rectTransform;

        if (enter)
        {
            rect.DOLocalMove(initialPos + hoverOffset, 0.1f);
            rect.DOScale(initialScale * hoverScale, 0.1f);
            btnText.DOColor(hoverColor, 0.1f);
            rect.DOLocalRotate(new Vector3(0, 0, 3f), 0.1f); // 旋轉 15 度
        }
        else
        {
            rect.DOLocalMove(initialPos, 0.1f);
            rect.DOScale(initialScale, 0.1f);
            btnText.DOColor(initialColor, 0.1f);
            rect.DOLocalRotate(Vector3.zero, 0.1f); // 回到初始角度
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPauseOpen) ClosePausePanel();
            else OpenPausePanel();
        }
    }

    // ===================== PausePanel 動畫 =====================
    public void OpenPausePanel()
    {
        if (isPauseOpen || pausePanel == null) return;

        pausePanel.gameObject.SetActive(true);
        pausePanel.localScale = Vector3.zero;
        pausePanel.localEulerAngles = new Vector3(0, 0, -90);

        if (menuButton != null)
        {
            menuButton.gameObject.SetActive(true);
            menuButton.transform.SetAsLastSibling();
            StartCoroutine(DropMenuButtonText());
        }

        Sequence seq = DOTween.Sequence();
        seq.Append(pausePanel.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack));
        seq.Join(pausePanel.DOLocalRotate(Vector3.zero, animationDuration));
        seq.Play();

        isPauseOpen = true;
    }

    public void ClosePausePanel()
    {
        if (!isPauseOpen || pausePanel == null) return;

        Sequence seq = DOTween.Sequence();
        seq.Append(pausePanel.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack));
        seq.Join(pausePanel.DOLocalRotate(new Vector3(0, 0, -90), animationDuration));
        seq.OnComplete(() =>
        {
            pausePanel.gameObject.SetActive(false);
            if (menuButton != null) menuButton.gameObject.SetActive(false);
            if (menuPanel != null) menuPanel.gameObject.SetActive(false);

            pausePanel.localPosition = pausePanelInitialPos;
            pausePanel.localEulerAngles = pausePanelInitialRot;
            isMenuOpen = false;
        });
        seq.Play();

        isPauseOpen = false;
    }

    // ===================== MenuPanel 切換 =====================
    public void ToggleMenuPanel()
    {
        if (isMenuOpen) CloseMenuPanel();
        else OpenMenuPanel();
    }

    private void OpenMenuPanel()
    {
        if (isMenuOpen || menuPanel == null) return;

        menuPanel.DOKill();
        menuButton?.DOKill();
        pausePanel?.DOKill();

        menuPanel.gameObject.SetActive(true);

        if (menuButtonText != null)
            menuButtonText.color = Color.white;

        menuButton?.SetAsLastSibling();

        Sequence seq = DOTween.Sequence();
        menuPanel.localScale = Vector3.zero;
        seq.Append(menuPanel.DOScale(Vector3.one * menuPanelTargetScale, animationDuration).SetEase(Ease.OutBack));

        if (menuButton != null)
            seq.Join(menuButton.DOLocalMoveY(menuButtonInitialLocalY + menuButtonMoveY, animationDuration)
                .SetEase(Ease.OutBack));

        if (pausePanel != null)
        {
            seq.Join(pausePanel.DOLocalMove(pausePanelInitialPos + pausePanelMenuOffset, animationDuration)
                .SetEase(Ease.OutBack));
            seq.Join(pausePanel.DOLocalRotate(new Vector3(0, 0, pausePanelMenuRotateZ), animationDuration)
                .SetEase(Ease.OutBack));
        }

        seq.Play();
        isMenuOpen = true;
    }

    private void CloseMenuPanel()
    {
        if (!isMenuOpen || menuPanel == null) return;

        menuPanel.DOKill();
        menuButton?.DOKill();
        pausePanel?.DOKill();

        if (menuButtonText != null)
            menuButtonText.color = Color.black;

        Sequence seq = DOTween.Sequence();
        seq.Append(menuPanel.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack));

        if (menuButton != null)
            seq.Join(menuButton.DOLocalMoveY(menuButtonInitialLocalY, animationDuration).SetEase(Ease.InBack));

        if (pausePanel != null)
        {
            seq.Join(pausePanel.DOLocalMove(pausePanelInitialPos, animationDuration).SetEase(Ease.InBack));
            seq.Join(pausePanel.DOLocalRotate(pausePanelInitialRot, animationDuration).SetEase(Ease.InBack));
        }

        seq.OnComplete(() => menuPanel.gameObject.SetActive(false));
        seq.Play();

        isMenuOpen = false;
    }

    // ===================== 逐字掉落 =====================
    private IEnumerator DropMenuButtonText()
    {
        menuButtonText.ForceMeshUpdate();
        TMP_TextInfo textInfo = menuButtonText.textInfo;

        // 儲存每個字的原始頂點位置
        Vector3[][] originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
            originalVertices[i] = (Vector3[])textInfo.meshInfo[i].vertices.Clone();

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // 從頂部 dropDistance 掉下來
            Vector3 startOffset = new Vector3(0, dropDistance, 0);
            for (int j = 0; j < 4; j++)
                vertices[vertexIndex + j] = originalVertices[materialIndex][vertexIndex + j] + startOffset;

            menuButtonText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            // DOTween 動畫
            DOTween.To(() => dropDistance, x =>
            {
                for (int j = 0; j < 4; j++)
                    vertices[vertexIndex + j] = originalVertices[materialIndex][vertexIndex + j] + new Vector3(0, x, 0);
                menuButtonText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }, 0, dropDuration);

            yield return new WaitForSeconds(dropDelayBetween);
        }
    }
}