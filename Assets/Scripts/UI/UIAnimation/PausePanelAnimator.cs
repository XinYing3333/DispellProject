using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class PausePanelAnimator : MonoBehaviour
{
    [Header("主要面板")] public RectTransform pausePanel;

    [Header("Menu 按鈕 & 面板")] public Button menuButton;
    public RectTransform menuPanel;
    public TextMeshProUGUI menuButtonText;

    [Header("Option 按鈕 & 面板")] public Button optionButton;
    public RectTransform optionPanel;
    public TextMeshProUGUI optionButtonText;

    [Header("Continue 按鈕")] public Button continueButton;
    public TextMeshProUGUI continueText;

    [Header("Quit 按鈕 & 面板")] public Button quitButton;
    public RectTransform quitPanel;
    public TextMeshProUGUI quitButtonText;

    [Header("MenuPanel 子物件")] public Button yesButton;
    public Button noButton;

    [Header("QuitPanel 子物件")] public Button yesButton_Quit;
    public Button noButton_Quit;

    private TextMeshProUGUI yesQuitText, noQuitText;
    private Vector3 yesQuitTextInitialPos, noQuitTextInitialPos;
    private Vector3 yesQuitTextInitialScale, noQuitTextInitialScale;
    private Color yesQuitTextInitialColor, noQuitTextInitialColor;


    [Header("動畫設定")] public float animationDuration = 0.3f;
    public float menuButtonMoveY = 100f;
    public float menuPanelTargetScale = 0.35f;
    public float optionPanelTargetScale = 0.35f;
    public Vector3 pausePanelMenuOffset = new Vector3(-50f, 0f, 0f);
    public float pausePanelMenuRotateZ = 10f;
    public Vector3 pausePanelOptionOffset = new Vector3(-100f, 0f, 0f);
    public float pausePanelOptionRotateZ = 15f;

    [Header("按鈕文字下墜動畫")] public float dropInDistance = 50f;
    public float dropInDuration = 0.5f;
    public float dropInDelayBetween = 0.05f;
    public float dropOutDistance = 50f;
    public float dropOutDuration = 0.5f;
    public float dropOutDelayBetween = 0.05f;

    [Header("Hover 動畫設定")] public float hoverScale = 1.1f;
    public Vector3 hoverOffset = new Vector3(5f, 5f, 0);
    public Color hoverColor = Color.yellow;

    private bool isPauseOpen = false;
    private bool isMenuOpen = false;
    private bool isOptionOpen = false;
    private bool isQuitOpen = false;

    private float menuButtonInitialLocalY;
    private float optionButtonInitialLocalY;
    private Vector3 pausePanelInitialPos;
    private Vector3 pausePanelInitialRot;

    // Continue 文字
    private Outline continueOutline;
    private Color continueInitialColor;

    // Yes/No 按鈕文字
    private TextMeshProUGUI yesText, noText;
    private Vector3 yesTextInitialPos, noTextInitialPos;
    private Vector3 yesTextInitialScale, noTextInitialScale;
    private Color yesTextInitialColor, noTextInitialColor;

    // OptionPanel 目標位置
    private Vector3 optionPanelTargetPos;

    void Start()
    {
        // PausePanel 初始
        if (pausePanel != null)
        {
            pausePanelInitialPos = pausePanel.localPosition;
            pausePanelInitialRot = pausePanel.localEulerAngles;
            pausePanel.localScale = Vector3.zero;
            pausePanel.localEulerAngles = new Vector3(0, 0, -90);
            pausePanel.gameObject.SetActive(false);
        }

        // MenuButton
        if (menuButton != null)
        {
            menuButtonInitialLocalY = menuButton.transform.localPosition.y;
            menuButton.gameObject.SetActive(false);
            menuButton.onClick.AddListener(ToggleMenuPanel);
        }

        // OptionButton
        if (optionButton != null)
        {
            optionButtonInitialLocalY = optionButton.transform.localPosition.y;
            optionButton.gameObject.SetActive(false);
            optionButton.onClick.AddListener(ToggleOptionPanel);
        }

        // MenuPanel
        if (menuPanel != null)
        {
            menuPanel.localScale = Vector3.zero;
            menuPanel.gameObject.SetActive(false);
        }

        // OptionPanel
        if (optionPanel != null)
        {
            optionPanelTargetPos = optionPanel.localPosition;
            optionPanel.localScale = Vector3.zero;
            optionPanel.gameObject.SetActive(false);
        }

        // Yes/No 按鈕初始化
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnMenuYesClicked);
            InitializeYesNoButton(yesButton, out yesText, out yesTextInitialPos, out yesTextInitialScale,
                out yesTextInitialColor);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(CloseMenuPanel);
            InitializeYesNoButton(noButton, out noText, out noTextInitialPos, out noTextInitialScale,
                out noTextInitialColor);
        }

        // Continue 初始化
        if (continueButton != null) InitializeContinueButton();

        // Quit 初始化
        if (quitPanel != null)
        {
            quitPanel.localScale = Vector3.zero;
            quitPanel.gameObject.SetActive(false);
        }

        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(false);
            quitButton.onClick.AddListener(ToggleQuitPanel);
        }
        // QuitPanel Yes/No 初始化

        if (yesButton_Quit != null)
            InitializeYesNoQuitButton_Quit(yesButton_Quit, out yesQuitText, out yesQuitTextInitialPos,
                out yesQuitTextInitialScale, out yesQuitTextInitialColor);

        if (noButton_Quit != null)
            InitializeYesNoQuitButton_Quit(noButton_Quit, out noQuitText, out noQuitTextInitialPos,
                out noQuitTextInitialScale, out noQuitTextInitialColor);
    }

    private void OnMenuYesClicked()
    {
        Debug.Log("回到首頁");
        // 這裡以後可以換成場景切換或其他功能
    }

    private void InitializeYesNoQuitButton_Quit(Button btn, out TextMeshProUGUI btnText, out Vector3 initialPos,
        out Vector3 initialScale, out Color initialColor)
    {
        btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
        initialPos = btnText.rectTransform.localPosition;
        initialScale = btnText.rectTransform.localScale;
        initialColor = btnText.color;

        AddHoverEvent(btn, btnText);

        if (btn == yesButton_Quit)
        {
            btn.onClick.AddListener(() =>
            {
                Debug.Log("退出遊戲");
                CloseQuitPanel(); // 收回 QuitPanel
                // Application.Quit(); // 遊戲發佈時啟用
            });
        }
        else if (btn == noButton_Quit)
        {
            btn.onClick.AddListener(() => CloseQuitPanel());
        }
    }


    private void InitializeYesNoButton(Button btn, out TextMeshProUGUI btnText, out Vector3 initialPos,
        out Vector3 initialScale, out Color initialColor)
    {
        btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
        initialPos = btnText.rectTransform.localPosition;
        initialScale = btnText.rectTransform.localScale;
        initialColor = btnText.color;
        btn.onClick.AddListener(CloseMenuPanel);
        AddHoverEvent(btn, btnText);
    }

    private void InitializeContinueButton()
    {
        continueText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
        continueInitialColor = Color.black;

        continueOutline = continueText.gameObject.AddComponent<Outline>();
        //continueOutline.effectColor = Color.black;
        continueOutline.enabled = false;

        EventTrigger trigger = continueButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = continueButton.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((data) => OnContinueHover(true));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((data) => OnContinueHover(false));
        trigger.triggers.Add(exit);

        continueButton.onClick.AddListener(ClosePausePanel);
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
            rect.DOLocalRotate(new Vector3(0, 0, 3f), 0.1f);
        }
        else
        {
            rect.DOLocalMove(initialPos, 0.1f);
            rect.DOScale(initialScale, 0.1f);
            btnText.DOColor(initialColor, 0.1f);
            rect.DOLocalRotate(Vector3.zero, 0.1f);
        }
    }

    private void OnContinueHover(bool enter)
    {
        if (continueText == null) return;
        continueText.color = enter ? Color.white : continueInitialColor;
        continueOutline.enabled = enter;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPauseOpen)
            {
                // 只有在沒有其他子面板開啟時才關閉 PausePanel
                if (!isMenuOpen && !isOptionOpen && !isQuitOpen)
                    ClosePausePanel();
            }
            else
            {
                OpenPausePanel();
            }
        }
    }


    private IEnumerator ClosePauseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClosePausePanel();
    }

    #region PausePanel

    public void OpenPausePanel()
    {
        if (isPauseOpen || pausePanel == null) return;

        pausePanel.gameObject.SetActive(true);
        pausePanel.localScale = Vector3.zero;
        pausePanel.localEulerAngles = new Vector3(0, 0, -90);

        if (menuButton != null) menuButton.gameObject.SetActive(true);
        if (optionButton != null) optionButton.gameObject.SetActive(true);
        if (continueButton != null) continueButton.gameObject.SetActive(true);
        if (quitButton != null) quitButton.gameObject.SetActive(true);

        StartCoroutine(DropButtonText(menuButtonText, true, dropInDistance, dropInDuration, dropInDelayBetween));
        StartCoroutine(DropButtonText(optionButtonText, true, dropInDistance, dropInDuration, dropInDelayBetween));
        StartCoroutine(DropButtonText(continueText, true, dropInDistance, dropInDuration, dropInDelayBetween));
        if (quitButtonText != null)
            StartCoroutine(DropButtonText(quitButtonText, true, dropInDistance, dropInDuration, dropInDelayBetween));

        Sequence seq = DOTween.Sequence();
        seq.Append(pausePanel.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack));
        seq.Join(pausePanel.DOLocalRotate(Vector3.zero, animationDuration));
        seq.Play();

        isPauseOpen = true;
    }

    public void ClosePausePanel()
    {
        if (!isPauseOpen || pausePanel == null) return;

        StartCoroutine(DropButtonText(menuButtonText, false, dropOutDistance, dropOutDuration, dropOutDelayBetween));
        StartCoroutine(DropButtonText(optionButtonText, false, dropOutDistance, dropOutDuration, dropOutDelayBetween));
        StartCoroutine(DropButtonText(continueText, false, dropOutDistance, dropOutDuration, dropOutDelayBetween));
        if (quitButtonText != null)
            StartCoroutine(DropButtonText(quitButtonText, false, dropOutDistance, dropOutDuration,
                dropOutDelayBetween));

        Sequence seq = DOTween.Sequence();
        seq.Append(pausePanel.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack));
        seq.Join(pausePanel.DOLocalRotate(new Vector3(0, 0, -90), animationDuration));
        seq.OnComplete(() =>
        {
            pausePanel.gameObject.SetActive(false);
            menuButton?.gameObject.SetActive(false);
            optionButton?.gameObject.SetActive(false);
            continueButton?.gameObject.SetActive(false);
            quitButton?.gameObject.SetActive(false);
            pausePanel.localPosition = pausePanelInitialPos;
            pausePanel.localEulerAngles = pausePanelInitialRot;
            isMenuOpen = isOptionOpen = isQuitOpen = false;
        });
        seq.Play();

        isPauseOpen = false;
    }

    #endregion

    #region MenuPanel

    public void ToggleMenuPanel()
    {
        if (isMenuOpen) CloseMenuPanel();
        else OpenMenuPanel();
    }

    private void OpenMenuPanel()
    {
        if (menuPanel == null || isMenuOpen) return;

        // 關閉其他面板
        if (isOptionOpen) CloseOptionPanel();
        if (isQuitOpen) CloseQuitPanel();

        menuPanel.DOKill(true); // 立即完成前一次 Tween
        menuPanel.gameObject.SetActive(true);
        menuPanel.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(menuPanel.DOScale(Vector3.one * menuPanelTargetScale, animationDuration).SetEase(Ease.OutBack));
        if (menuButton != null)
            seq.Join(menuButton.transform.DOLocalMoveY(menuButtonInitialLocalY + menuButtonMoveY, animationDuration)
                .SetEase(Ease.OutBack));
        if (pausePanel != null)
        {
            seq.Join(pausePanel.DOLocalMove(pausePanelInitialPos + pausePanelMenuOffset, animationDuration)
                .SetEase(Ease.OutBack));
            seq.Join(pausePanel.DOLocalRotate(new Vector3(0, 0, pausePanelMenuRotateZ), animationDuration)
                .SetEase(Ease.OutBack));
        }

        if (menuButtonText != null)
            menuButtonText.DOColor(Color.white, 0.1f);

        seq.OnComplete(() =>
        {
            isMenuOpen = true;
            if (menuButtonText != null) menuButtonText.color = Color.white;
        });
    }

    private void CloseMenuPanel()
    {
        if (menuPanel == null || !isMenuOpen) return;

        menuPanel.DOKill(true); // 立即完成前一次 Tween

        Sequence seq = DOTween.Sequence();
        seq.Append(menuPanel.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack));
        if (menuButton != null)
            seq.Join(menuButton.transform.DOLocalMoveY(menuButtonInitialLocalY, animationDuration)
                .SetEase(Ease.InBack));
        if (pausePanel != null)
        {
            seq.Join(pausePanel.DOLocalMove(pausePanelInitialPos, animationDuration).SetEase(Ease.InBack));
            seq.Join(pausePanel.DOLocalRotate(pausePanelInitialRot, animationDuration).SetEase(Ease.InBack));
        }

        if (menuButtonText != null)
            menuButtonText.DOColor(Color.black, 0.1f);


        seq.OnComplete(() =>
        {
            menuPanel.gameObject.SetActive(false);
            isMenuOpen = false;
            if (menuButtonText != null) menuButtonText.color = Color.black; // 動畫完成後改字色
        });
    }

    #endregion

    #region OptionPanel

    public void ToggleOptionPanel()
    {
        if (isOptionOpen) CloseOptionPanel();
        else OpenOptionPanel();
    }

    private bool optionAnimPlaying = false;

    private void OpenOptionPanel()
    {
        if (optionPanel == null || isOptionOpen || optionAnimPlaying) return;

        // 關閉其他面板
        if (isMenuOpen) CloseMenuPanel();
        if (isQuitOpen) CloseQuitPanel();

        optionPanel.DOKill(true);
        optionPanel.gameObject.SetActive(true);
        optionPanel.localScale = Vector3.zero;
        optionPanel.localPosition = optionPanelTargetPos;

        optionAnimPlaying = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(optionPanel.DOScale(Vector3.one * optionPanelTargetScale, animationDuration).SetEase(Ease.OutBack));
        if (optionButton != null)
            seq.Join(optionButton.transform.DOLocalMoveY(optionButtonInitialLocalY + menuButtonMoveY, animationDuration)
                .SetEase(Ease.OutBack));
        if (pausePanel != null)
        {
            seq.Join(pausePanel.DOLocalMove(pausePanelInitialPos + pausePanelMenuOffset, animationDuration)
                .SetEase(Ease.OutBack));
            seq.Join(pausePanel.DOLocalRotate(new Vector3(0, 0, pausePanelMenuRotateZ), animationDuration)
                .SetEase(Ease.OutBack));
        }

        if (optionButtonText != null)
            seq.Join(optionButtonText.DOColor(Color.white, 0.1f));

        seq.OnComplete(() =>
        {
            isOptionOpen = true;
            optionAnimPlaying = false;
            if (optionButtonText != null) optionButtonText.color = Color.white;
        });
    }

    private void CloseOptionPanel()
    {
        if (!isOptionOpen || optionAnimPlaying || optionPanel == null) return;

        optionAnimPlaying = true;

        Sequence seq = DOTween.Sequence();
        // 縮小 OptionPanel
        seq.Append(optionPanel.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack));
        // OptionButton 回原位
        if (optionButton != null)
            seq.Join(optionButton.transform.DOLocalMoveY(optionButtonInitialLocalY, animationDuration)
                .SetEase(Ease.InBack));
        // pausePanel 回原位 & 回原旋轉
        if (pausePanel != null)
        {
            seq.Join(pausePanel.DOLocalMove(pausePanelInitialPos, animationDuration).SetEase(Ease.InBack));
            seq.Join(pausePanel.DOLocalRotate(pausePanelInitialRot, animationDuration).SetEase(Ease.InBack));
        }

        // 按鈕文字變黑
        if (optionButtonText != null)
            seq.Join(optionButtonText.DOColor(Color.black, 0.1f));

        seq.OnComplete(() =>
        {
            optionPanel.gameObject.SetActive(false);
            isOptionOpen = false;
            optionAnimPlaying = false;
            if (optionButtonText != null) optionButtonText.color = Color.black;
        });
    }

    #endregion


    #region QuitPanel

    public void ToggleQuitPanel()
    {
        if (isQuitOpen) CloseQuitPanel();
        else OpenQuitPanel();
    }

    private void OpenQuitPanel()
    {
        if (quitPanel == null || isQuitOpen) return;

        quitPanel.DOKill(true);
        quitButton?.transform.DOKill();
        pausePanel?.DOKill();

        quitPanel.gameObject.SetActive(true);
        quitPanel.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(quitPanel.DOScale(Vector3.one * menuPanelTargetScale, animationDuration).SetEase(Ease.OutBack));
        if (quitButton != null)
            seq.Join(quitButton.transform.DOLocalMoveY(menuButtonInitialLocalY + menuButtonMoveY, animationDuration)
                .SetEase(Ease.OutBack));
        if (pausePanel != null)
        {
            seq.Join(pausePanel.DOLocalMove(pausePanelInitialPos + pausePanelMenuOffset, animationDuration)
                .SetEase(Ease.OutBack));
            seq.Join(pausePanel.DOLocalRotate(new Vector3(0, 0, pausePanelMenuRotateZ), animationDuration));
        }

        // Quit 文字變色，0.1秒快速完成
        if (quitButtonText != null)
            quitButtonText.DOColor(Color.white, 0.1f);

        seq.OnComplete(() =>
        {
            isQuitOpen = true;
            if (quitButtonText != null) quitButtonText.color = Color.white; // 動畫完成後改字色
        });
    }

    private void CloseQuitPanel()
    {
        if (!isQuitOpen || quitPanel == null) return;

        quitPanel.DOKill(true);
        quitButton?.transform.DOKill();
        pausePanel?.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(quitPanel.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack));
        if (quitButton != null)
            seq.Join(quitButton.transform.DOLocalMoveY(menuButtonInitialLocalY, animationDuration)
                .SetEase(Ease.InBack));
        if (pausePanel != null)
        {
            seq.Join(pausePanel.DOLocalMove(pausePanelInitialPos, animationDuration).SetEase(Ease.InBack));
            seq.Join(pausePanel.DOLocalRotate(pausePanelInitialRot, animationDuration).SetEase(Ease.InBack));
        }

        // Quit 文字立即變回黑色
        if (quitButtonText != null)
            quitButtonText.DOColor(Color.black, 0.1f);

        seq.OnComplete(() =>
        {
            quitPanel.gameObject.SetActive(false);
            isQuitOpen = false;
            if (quitButtonText != null) quitButtonText.color = Color.black; // 動畫完成後改字色
        });

        seq.Play();
    }

    #endregion

    #region 文字下墜動畫

    private IEnumerator DropButtonText(TextMeshProUGUI text, bool isDropIn, float distance, float duration,
        float delayBetween)
    {
        if (text == null) yield break;

        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;
        Vector3[][] originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
            originalVertices[i] = (Vector3[])textInfo.meshInfo[i].vertices.Clone();

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            float startY = isDropIn ? distance : 0f;
            float endY = isDropIn ? 0f : -distance * 2f;

            if (isDropIn)
            {
                for (int j = 0; j < 4; j++)
                    vertices[vertexIndex + j] =
                        originalVertices[materialIndex][vertexIndex + j] + new Vector3(0, startY, 0);
                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }

            DOTween.To(() => startY, y =>
            {
                for (int j = 0; j < 4; j++)
                    vertices[vertexIndex + j] = originalVertices[materialIndex][vertexIndex + j] + new Vector3(0, y, 0);
                text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }, endY, duration);

            yield return new WaitForSeconds(delayBetween);
        }

        yield return new WaitForSeconds(duration);
    }

    #endregion
}