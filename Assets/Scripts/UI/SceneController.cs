using Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    
    [SerializeField]private GameObject settingPanel;
    [SerializeField]private CanvasGroup settingCanvasGroup;
    [SerializeField] private GameObject settingFirstButton;
    
    [SerializeField] private GameObject skillPanel; 
    [SerializeField]private CanvasGroup skillCanvasGroup;
    [SerializeField] private GameObject skillFirstButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        switch (sceneName)
        {
            case "MainMenu":
                AudioManager.Instance.PlayBGM(BGMType.MainMenu);
                AudioManager.Instance.OnSceneLoaded();
                
                settingCanvasGroup = settingPanel.GetComponent<CanvasGroup>();
                settingCanvasGroup.alpha = 0f;
                settingCanvasGroup.blocksRaycasts = false;
                settingCanvasGroup.interactable = false;
                
                break;
            case "Level1Main":
                AudioManager.Instance.PlayBGM(BGMType.FirstLevel);
                AudioManager.Instance.OnSceneLoaded();
                
                settingCanvasGroup = settingPanel.GetComponent<CanvasGroup>();
                skillCanvasGroup = skillPanel.GetComponent<CanvasGroup>();

                settingCanvasGroup.alpha = 0f;
                settingCanvasGroup.blocksRaycasts = false;
                settingCanvasGroup.interactable = false;
                skillCanvasGroup.alpha = 0f;
                skillCanvasGroup.blocksRaycasts = false;
                skillCanvasGroup.interactable = false;
                break;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) 
        {
            SceneManager.LoadScene("MainMenu");
        }
        if (Input.GetKeyDown(KeyCode.F2)) //Restart
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Transform checkPoint = GameObject.FindGameObjectWithTag("CheckPoint").transform;
            player.transform.position = checkPoint.position;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) //Close
        {
            QuitGame();
        }

        string sceneName = SceneManager.GetActiveScene().name;

        switch (sceneName)
        {
            case "MainMenu":
                SetSettingPanel();
                break;
            case "Level1Main":
                SetSkillPanel();
                SetSettingPanel();
                break;
        }
    }
    public static void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static void LoadSceneAsync(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
    
    public static void QuitGame()
    {
        Application.Quit();
    }

    private bool wasSettingOpen;
    public void SetSettingPanel()
    {
        bool nowOpen = PlayerInputHandler.Instance.IsSettingPressed;
        
        
        if (nowOpen && !wasSettingOpen)
        {
            SetSettingOpen();
            EventSystem.current.SetSelectedGameObject(settingFirstButton);
            Time.timeScale = 0f;
        }
        else if (!nowOpen && wasSettingOpen || !settingPanel.activeSelf)
        {
            SetSettingClose();
            Time.timeScale = 1f;
        }

        wasSettingOpen = nowOpen;
    }

    public void SetSettingOpen()
    {
        settingCanvasGroup.alpha = 1f;
        settingCanvasGroup.blocksRaycasts = true;
        settingCanvasGroup.interactable = true;
    }
    public void SetSettingClose()
    {
        settingCanvasGroup.alpha = 0f;
        settingCanvasGroup.blocksRaycasts = false;
        settingCanvasGroup.interactable = false;
    }
    
    private bool wasSkillOpen = false;
    public void SetSkillPanel()
    {
        bool nowOpen = PlayerInputHandler.Instance.IsSkillUIOpen;

        if (nowOpen && !wasSkillOpen)
        {
            skillCanvasGroup.alpha = 1f;
            skillCanvasGroup.blocksRaycasts = true;
            skillCanvasGroup.interactable = true;
            EventSystem.current.SetSelectedGameObject(skillFirstButton);
            Time.timeScale = 0.005f;
            Time.fixedDeltaTime = 0.2f * Time.timeScale; // 推薦做法
        }
        else if (!nowOpen && wasSkillOpen)
        {
            skillCanvasGroup.alpha = 0f;
            skillCanvasGroup.blocksRaycasts = false;
            skillCanvasGroup.interactable = false;
            Time.timeScale = 1f;
        }

        wasSkillOpen = nowOpen;
    }

    public void SoundOnClick()
    {
        AudioManager.Instance.PlaySFX(SFXType.Click);
    }
}
