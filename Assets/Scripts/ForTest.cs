using Cinemachine;
using DefaultNamespace;
using EventBus.Events.Health;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Player
{
    public class ForTest : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject CheatCanvas;
        public Transform[] areas;
        
        [Header("References")]
        public CinemachineFreeLook mainCam;
        public TextMeshProUGUI statusText;

        private GameObject player;
        private int areaIndex = 0;
        private bool invertY = false;

        // 用於長按判定
        private float cheatOpenTimer = 0f;
        private const float HOLD_TIME = 2.0f; // 長按 2 秒才開啟

        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (CheatCanvas) CheatCanvas.SetActive(false);
        }

        private void Update()
        {
            // 只有在開發模式下才運行
            #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
                return;
            #endif

            HandleActivation();

            if (CheatCanvas != null && CheatCanvas.activeSelf)
            {
                HandleCheatLogic();
            }
        }
        

        private int comboStep = 0; // 當前完成到第幾步
        private float comboTimer = 0f; // 組合鍵有效時間窗口
        private const float COMBO_WINDOW = 2.0f; // 必須在 2 秒內按完

        private void HandleActivation()
        {
            var input = PlayerInputHandler.Instance;

            // 1. 按下 Select 開啟偵測窗口並重置進度
            if (input.SettingLeftPressed)
            {
                comboStep = 0;
                comboTimer = COMBO_WINDOW;
            }

            if (comboTimer > 0)
            {
                comboTimer -= Time.unscaledDeltaTime;

                // 2. 偵測序列：左(0,1) -> 右(2,3)
                if (comboStep < 2) // 期待左鍵
                {
                    if (input.SettingRightPressed)
                    {
                        comboStep++;
                    }
                    else if (input.SelectPressed) // 按錯了，重置
                    {
                        comboStep = 0;
                    }
                }
                else if (comboStep < 4) // 期待右鍵
                {
                    if (input.SelectPressed)
                    {
                        comboStep++;
                    }
                    else if (input.SettingRightPressed) // 按錯了，重置
                    {
                        comboStep = 0;
                    }
                }

                // 3. 檢查是否完成
                if (comboStep == 4)
                {
                    ToggleCheatMenu();
                    comboStep = 0;
                    comboTimer = 0;
                }
            }
            else
            {
                comboStep = 0; // 超時重置
            }
        }

        private void ToggleCheatMenu()
        {
            bool isActive = !CheatCanvas.activeSelf;
            CheatCanvas.SetActive(isActive);

            // // 修改點 2：切換 Action Map 與 暫停
            // if (isActive)
            // {
            //     // 鎖定玩家移動，並強制切換到 UI Map (如果你有設定的話)
            //     PlayerInputHandler.Instance.SetPauseMode(true); 
            // }
            // else
            // {
            //     PlayerInputHandler.Instance.SetPauseMode(false);
            // }
        }
        
        private void HandleCheatLogic()
        {
            var input = PlayerInputHandler.Instance;

            // 1. 手動觸發重生事件 (新增)
            if (input.DashPressed)
            {
                Debug.Log("[Cheat] Manual Respawn Triggered.");
                EventBus<OnPlayerRespawn>.Raise(new OnPlayerRespawn());
            }

            // 2. 重置數據 (Select + Switch)
            if (input.SettingLeftPressed && input.SettingRightPressed)
            {
                PlayerPrefs.DeleteAll();
                Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            // 3. 地點切換 (左鍵逆序 / 右鍵順序)
            if (areas.Length > 0)
            {
                if (input.SettingRightPressed)
                {
                    // 順序切換
                    areaIndex = (areaIndex + 1) % areas.Length;
                    TeleportToArea();
                }
                else if (input.SettingLeftPressed)
                {
                    // 逆序切換 (加上 areas.Length 確保索引不為負數)
                    areaIndex = (areaIndex - 1 + areas.Length) % areas.Length;
                    TeleportToArea();
                }
            }
    
            // 5. 關閉選單
            if (input.ExitPressed)
            {
                ToggleCheatMenu();
            }
        }

        private void TeleportToArea()
        {
            if (player != null && areas[areaIndex] != null)
            {
                player.transform.position = areas[areaIndex].position;
                statusText.text = "Teleported to [{areaIndex}]: {areas[areaIndex].name}";
            }
        }
    }
}