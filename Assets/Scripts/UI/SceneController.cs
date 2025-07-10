using Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// 控制場景中的 UI 面板顯示與場景切換邏輯，支援設定面板與技能選擇面板。
/// </summary>
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Setting UI")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private CanvasGroup settingCanvasGroup;
    [SerializeField] private GameObject settingFirstButton;

    [Header("Skill UI")]
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private CanvasGroup skillCanvasGroup;
    [SerializeField] private GameObject skillFirstButton;

    private bool wasSettingOpen = false;
    private bool wasSkillOpen = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        // 播放對應場景的背景音樂，初始化 UI 面板狀態
        AudioManager.Instance.OnSceneLoaded();

        switch (sceneName)
        {
            case "MainMenu":
                AudioManager.Instance.PlayBGM(BGMType.MainMenu);
                InitCanvasGroup(settingPanel, ref settingCanvasGroup);
                break;

            case "Level1Main":
                AudioManager.Instance.PlayBGM(BGMType.FirstLevel);
                InitCanvasGroup(settingPanel, ref settingCanvasGroup);
                InitCanvasGroup(skillPanel, ref skillCanvasGroup);
                break;
        }
    }

    void Update()
    {
        // 快捷鍵測試切場景/重啟/重生/退出
        if (Input.GetKeyDown(KeyCode.F1)) SceneManager.LoadScene("MainMenu");
        if (Input.GetKeyDown(KeyCode.F2)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (Input.GetKeyDown(KeyCode.F3))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Transform checkPoint = GameObject.FindGameObjectWithTag("CheckPoint")?.transform;
            if (player && checkPoint) player.transform.position = checkPoint.position;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) QuitGame();

        // 控制面板開關
        string sceneName = SceneManager.GetActiveScene().name;
        //if (sceneName == "MainMenu") SetSettingPanelInMainMenu();
        //else 
        if (sceneName == "Level1Main")
        {
            SetSkillPanel();
            SetSettingPanel();
        }
    }

    public static void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
    public static void LoadSceneAsync(string sceneName) => SceneManager.LoadSceneAsync(sceneName);
    public static void QuitGame() => Application.Quit();

    /// <summary>
    /// 控制設定面板的顯示狀態
    /// </summary>
    public void SetSettingPanelInMainMenu()
    {
        if (!wasSettingOpen)
        {
            SetCanvasGroup(settingCanvasGroup, true);
            EventSystem.current?.SetSelectedGameObject(settingFirstButton);
        }
        else if (wasSettingOpen || !settingPanel.activeSelf)
        {
            SetCanvasGroup(settingCanvasGroup, false);
        }
        wasSettingOpen = !wasSettingOpen;
    }
    
    /// <summary>
    /// 控制設定面板的顯示狀態
    /// </summary>
    public void SetSettingPanel()
    {
        bool nowOpen = PlayerInputHandler.Instance.IsSettingPressed;

        if (nowOpen && !wasSettingOpen)
        {
            SetCanvasGroup(settingCanvasGroup, true);
            EventSystem.current?.SetSelectedGameObject(settingFirstButton);
            Time.timeScale = 0f;
        }
        else if (!nowOpen && wasSettingOpen || !settingPanel.activeSelf)
        {
            SetCanvasGroup(settingCanvasGroup, false);
            Time.timeScale = 1f;
        }

        wasSettingOpen = nowOpen;
    }

    /// <summary>
    /// 控制技能面板的顯示狀態
    /// </summary>
    public void SetSkillPanel()
    {
        bool nowOpen = PlayerInputHandler.Instance.IsSkillUIOpen;

        if (nowOpen && !wasSkillOpen)
        {
            SetCanvasGroup(skillCanvasGroup, true);
            EventSystem.current?.SetSelectedGameObject(skillFirstButton);
            Time.timeScale = 0.005f;
            Time.fixedDeltaTime = 0.2f * Time.timeScale;
        }
        else if (!nowOpen && wasSkillOpen)
        {
            SetCanvasGroup(skillCanvasGroup, false);
            Time.timeScale = 1f;
        }

        wasSkillOpen = nowOpen;
    }

    /// <summary>
    /// 播放 UI 點擊音效
    /// </summary>
    public void SoundOnClick() => AudioManager.Instance.PlaySFX(SFXType.Click);

    /// <summary>
    /// 快速設定 CanvasGroup 開關
    /// </summary>
    private void SetCanvasGroup(CanvasGroup group, bool isOn)
    {
        if (group == null) return;
        group.alpha = isOn ? 1f : 0f;
        group.blocksRaycasts = isOn;
        group.interactable = isOn;
    }

    /// <summary>
    /// 初始化 CanvasGroup 組件
    /// </summary>
    private void InitCanvasGroup(GameObject obj, ref CanvasGroup group)
    {
        if (obj != null)
        {
            if (!obj.TryGetComponent(out group))
                group = obj.AddComponent<CanvasGroup>();
            
            SetCanvasGroup(group, false);
        }
    }
}
