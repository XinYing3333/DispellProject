using Cinemachine;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    public class ForTest : MonoBehaviour
    {
        public GameObject CheatCanvas;
        public Transform[] areas;

        private GameObject player;
        private bool openCheatMode = false;
        private int areaIndex = 0;
        private bool prevJumpPressed;
        
        public CinemachineFreeLook mainCam; // 指向你的主攝影機
        public TextMeshProUGUI yAxisStatusText; // 顯示 Y 軸狀態
        private bool invertY = false;        // 狀態記錄


        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (CheatCanvas) CheatCanvas.SetActive(false);
            UpdateYAxisStatusText(); // 初始化顯示
        }

        private void Update()
        {
            var input = PlayerInputHandler.Instance;
            if (input == null) return;

            // 開關作弊介面
            if (input.ExitPressed)
            {
                openCheatMode = !openCheatMode;
                if (CheatCanvas) CheatCanvas.SetActive(openCheatMode);
            }

            if (!openCheatMode)
            {
                prevJumpPressed = false;
                return;
            }

            // 重置
            if (input.ResetPressed)
            {
                PlayerPrefs.DeleteAll();
                CollectionSystem.ClearCollection();
                ForTutorialDemo.isTutorialFinished = false;
                Time.timeScale = 1;
                SceneController.Instance.LoadScene("MainMenu");
                return;
            }

            // 每按一下跳躍切換地點
            bool jumpNow = input.InteractPressed;
            if (jumpNow && !prevJumpPressed && areas.Length > 0)
            {
                areaIndex = (areaIndex + 1) % areas.Length;
                player.transform.position = areas[areaIndex].position;
            }

            prevJumpPressed = jumpNow;
            
            // 切換 Y 軸反轉
            if (input.SwitchPressed)
            {
                invertY = !invertY;
                if (mainCam)
                {
                    var axis = mainCam.m_YAxis;
                    axis.m_InvertInput = invertY;
                    mainCam.m_YAxis = axis;
                }
                UpdateYAxisStatusText();
            }

        }
        
        private void UpdateYAxisStatusText()
        {
            if (yAxisStatusText == null) return;
            yAxisStatusText.text = invertY ? "Y Axis: Inverted" : "Y Axis: Normal";
        }
    }
}