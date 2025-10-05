using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using DG.Tweening;

public class OptionSettingManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionPanel;
    public GameObject audioPanel;
    public GameObject graphicPanel;
    public GameObject controlPanel;

    [Header("Page Transitions")]
    public float transitionDuration = 0.3f;

    [Header("Navigation Buttons (Page)")]
    public Button leftButton;
    public Button rightButton;

    [Header("Tab Buttons")]
    public Button audioTabButton;
    public Button graphicTabButton;
    public Button controlTabButton;

    private GameObject[] pages;
    private int currentPage = 0;

    [Header("Audio Settings")]
    public AudioMixer audioMixer;
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider musicSlider;

    [Header("Graphic Settings UI")]
    public TextMeshProUGUI resolutionText;
    public TextMeshProUGUI displayText;
    public TextMeshProUGUI qualityText;

    [Header("Graphic Buttons")]
    public Button resolutionLeftBtn;
    public Button resolutionRightBtn;
    public Button displayLeftBtn;
    public Button displayRightBtn;
    public Button qualityLeftBtn;
    public Button qualityRightBtn;

    [Header("Control Settings UI")]
    public Slider mouseSensitivitySlider;
    public Slider controllerSensitivitySlider;
    public Toggle invertYToggle;

    private int currentResolutionIndex;
    private int currentDisplayIndex; // 0 = Fullscreen, 1 = Windowed, 2 = Borderless
    private int currentQualityIndex;

    public float mouseSensitivity = 1f;
    public float controllerSensitivity = 1f;
    public bool invertY = false;

    private string[] resolutionOptions = { "1920x1080", "1600x900", "1280x720" };
    private FullScreenMode[] displayModes = { FullScreenMode.FullScreenWindow, FullScreenMode.Windowed, FullScreenMode.MaximizedWindow };
    private string[] qualityOptions = { "Low", "Medium", "High", "Ultra" };

    private void Start()
    {
        pages = new GameObject[] { audioPanel, graphicPanel, controlPanel };
        HideAllPages();
        ShowPage(currentPage);

        // OptionPanel 左右箭頭
        leftButton?.gameObject.SetActive(false);
        rightButton?.gameObject.SetActive(false);
        leftButton?.onClick.AddListener(PreviousPage);
        rightButton?.onClick.AddListener(NextPage);

        // Tab 按鈕
        audioTabButton?.onClick.AddListener(() => ShowPageByIndex(0));
        graphicTabButton?.onClick.AddListener(() => ShowPageByIndex(1));
        controlTabButton?.onClick.AddListener(() => ShowPageByIndex(2));

        // Audio
        SetupAudio();

        // Graphic
        SetupGraphic();
        resolutionLeftBtn?.onClick.AddListener(() => ChangeResolution(-1));
        resolutionRightBtn?.onClick.AddListener(() => ChangeResolution(1));
        displayLeftBtn?.onClick.AddListener(() => ChangeDisplay(-1));
        displayRightBtn?.onClick.AddListener(() => ChangeDisplay(1));
        qualityLeftBtn?.onClick.AddListener(() => ChangeQuality(-1));
        qualityRightBtn?.onClick.AddListener(() => ChangeQuality(1));

        // Control
        SetupControl();
    }

    #region OptionPanel Control
public void OpenOptionPanel()
{
    if (optionPanel != null)
        optionPanel.SetActive(true);

    // 重置頁面到第一頁
    currentPage = 0;

    // 隱藏所有頁面，然後顯示第一頁
    HideAllPages();
    if (pages != null && pages.Length > 0 && pages[0] != null)
        pages[0].SetActive(true);

    // 保證箭頭出現
    if (leftButton != null)
        leftButton.gameObject.SetActive(true);
    if (rightButton != null)
        rightButton.gameObject.SetActive(true);

    UpdateArrowButtons(); // 初始化互動狀態

    // Graphic 頁面文字刷新（如果第一頁是 Audio 可以跳過）
    if (currentPage == 1) RefreshGraphicTexts();
}



public void CloseOptionPanel()
{
    if (optionPanel != null)
        optionPanel.SetActive(false);

    if (leftButton != null)
        leftButton.gameObject.SetActive(false);
    if (rightButton != null)
        rightButton.gameObject.SetActive(false);

    Debug.Log("Option Panel 關閉了");
}


private void UpdateArrowButtons()
{
    if (optionPanel == null || !optionPanel.activeSelf)
        return;

    // 保持顯示，只改「是否可互動」
    if (leftButton != null)
        leftButton.interactable = currentPage > 0;
    if (rightButton != null)
        rightButton.interactable = currentPage < pages.Length - 1;
}
#endregion


    #region Page Navigation
    public void NextPage()
    {
        if (currentPage < pages.Length - 1)
            ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
            ShowPage(currentPage - 1);
    }

private void ShowPage(int index)
{
    if (index == currentPage) return;

    int previousPage = currentPage;
    currentPage = Mathf.Clamp(index, 0, pages.Length - 1);

    GameObject previousObj = pages[previousPage];
    GameObject newObj = pages[currentPage];

    RectTransform prevRect = previousObj?.GetComponent<RectTransform>();
    RectTransform newRect = newObj?.GetComponent<RectTransform>();

    if (prevRect == null || newRect == null)
    {
        HideAllPages();
        newObj?.SetActive(true);
        UpdateArrowButtons();
        if (currentPage == 1) RefreshGraphicTexts();
        return;
    }

    // 確保 CanvasGroup 存在
    CanvasGroup prevCG = previousObj.GetComponent<CanvasGroup>();
    if (prevCG == null) prevCG = previousObj.AddComponent<CanvasGroup>();
    CanvasGroup newCG = newObj.GetComponent<CanvasGroup>();
    if (newCG == null) newCG = newObj.AddComponent<CanvasGroup>();

    float width = prevRect.rect.width;
    Vector2 exitPos = new Vector2(index > previousPage ? -width : width, 0);
    Vector2 enterPos = new Vector2(index > previousPage ? width : -width, 0);
    Vector2 centerPos = Vector2.zero;

    // 新頁初始化位置 + 透明度 0
    newRect.anchoredPosition = enterPos;
    newCG.alpha = 0f;
    newObj.SetActive(true);

    Sequence seq = DOTween.Sequence();

    // 舊頁滑出 + 淡出
    seq.Append(prevRect.DOAnchorPos(exitPos, transitionDuration).SetEase(Ease.InCubic));
    seq.Join(prevCG.DOFade(0f, transitionDuration));

    // 舊頁隱藏 & 重置透明度
    seq.AppendCallback(() => 
    {
        previousObj.SetActive(false);
        prevCG.alpha = 1f; // 為下次使用重置
    });

    // 新頁滑入 + 淡入
    seq.Append(newRect.DOAnchorPos(centerPos, transitionDuration).SetEase(Ease.OutCubic));
    seq.Join(newCG.DOFade(1f, transitionDuration));

    // 更新箭頭和 Graphic 文本
    seq.AppendCallback(() =>
    {
        UpdateArrowButtons();
        if (currentPage == 1) RefreshGraphicTexts();
    });
}



    private void ShowPageByIndex(int index) => ShowPage(index);

    private void HideAllPages()
{
    foreach (var page in pages)
    {
        if (page != null)
        {
            page.SetActive(false);
            RectTransform rt = page.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero; // ★ 強制歸零
        }
    }
}

    #endregion

    #region Audio
    private void SetupAudio()
    {
        masterSlider?.onValueChanged.AddListener(SetMasterVolume);
        sfxSlider?.onValueChanged.AddListener(SetSFXVolume);
        musicSlider?.onValueChanged.AddListener(SetMusicVolume);
    }

    private void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        Debug.Log("Master Volume: " + value);
    }

    private void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        Debug.Log("SFX Volume: " + value);
    }

    private void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
        Debug.Log("Music Volume: " + value);
    }
    #endregion

    #region Graphic
    private void SetupGraphic()
    {
        currentResolutionIndex = 0;
        currentDisplayIndex = Screen.fullScreen ? 0 : (Screen.fullScreenMode == FullScreenMode.MaximizedWindow ? 2 : 1);
        currentQualityIndex = QualitySettings.GetQualityLevel();

        RefreshGraphicTexts();
    }

    private void RefreshGraphicTexts()
    {
        if (resolutionText != null)
            resolutionText.text = resolutionOptions[currentResolutionIndex];
        if (displayText != null)
            displayText.text = displayModes[currentDisplayIndex] == FullScreenMode.FullScreenWindow ? "Fullscreen" :
                               displayModes[currentDisplayIndex] == FullScreenMode.Windowed ? "Windowed" : "Borderless";
        if (qualityText != null)
            qualityText.text = qualityOptions[currentQualityIndex];
    }

    private void ChangeResolution(int delta)
    {
        currentResolutionIndex = (currentResolutionIndex + delta + resolutionOptions.Length) % resolutionOptions.Length;
        ApplyGraphicSettings();
        RefreshGraphicTexts();
        Debug.Log("Resolution: " + resolutionOptions[currentResolutionIndex]);
    }

    private void ChangeDisplay(int delta)
    {
        currentDisplayIndex = (currentDisplayIndex + delta + displayModes.Length) % displayModes.Length;
        ApplyGraphicSettings();
        RefreshGraphicTexts();
        Debug.Log("Display: " + (displayModes[currentDisplayIndex] == FullScreenMode.FullScreenWindow ? "Fullscreen" :
                                   displayModes[currentDisplayIndex] == FullScreenMode.Windowed ? "Windowed" : "Borderless"));
    }

    private void ChangeQuality(int delta)
    {
        currentQualityIndex = (currentQualityIndex + delta + qualityOptions.Length) % qualityOptions.Length;
        ApplyGraphicSettings();
        RefreshGraphicTexts();
        Debug.Log("Quality: " + qualityOptions[currentQualityIndex]);
    }

    private void ApplyGraphicSettings()
    {
        string[] resSplit = resolutionOptions[currentResolutionIndex].Split('x');
        int width = int.Parse(resSplit[0]);
        int height = int.Parse(resSplit[1]);
        Screen.SetResolution(width, height, displayModes[currentDisplayIndex]);
        QualitySettings.SetQualityLevel(currentQualityIndex);
    }
    #endregion

    #region Control
    private void SetupControl()
    {
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = mouseSensitivity;
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        }

        if (controllerSensitivitySlider != null)
        {
            controllerSensitivitySlider.value = controllerSensitivity;
            controllerSensitivitySlider.onValueChanged.AddListener(SetControllerSensitivity);
        }

        if (invertYToggle != null)
        {
            invertYToggle.isOn = invertY;
            invertYToggle.onValueChanged.AddListener(SetInvertY);
        }
    }

    private void SetMouseSensitivity(float value)
    {
        mouseSensitivity = value;
        Debug.Log("Mouse Sensitivity: " + mouseSensitivity);
    }

    private void SetControllerSensitivity(float value)
    {
        controllerSensitivity = value;
        Debug.Log("Controller Sensitivity: " + controllerSensitivity);
    }

    private void SetInvertY(bool value)
    {
        invertY = value;
        Debug.Log("Invert Y-axis: " + invertY);
    }
    #endregion

    

}
