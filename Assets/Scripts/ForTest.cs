using Cinemachine;
using DefaultNamespace;
using EventBus.Events.Health;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    public class ForTest : MonoBehaviour
    {
        public GameObject CheatCanvas;
        public Transform[] areas;

        public RespawnController respawn;
        private GameObject player;
        private bool openCheatMode = false;
        private int areaIndex = 0;
        private bool prevJumpPressed;


        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (CheatCanvas) CheatCanvas.SetActive(false);
            respawn = player.GetComponent<RespawnController>();
        }

        private void Update()
        {
            var input = PlayerInputHandler.Instance;
            if (input == null) return;

            // 開關作弊介面
            if (PlayerInputHandler.Instance.ExitPressed)
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
            if (PlayerInputHandler.Instance.SelectPressed)
            {
                PlayerPrefs.DeleteAll();
                //ForTutorialDemo.isTutorialFinished = false;
                Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
            
            if (input.SwitchPressed)
            {
                //respawn.RespawnAtLastSafe();
            }

            if (Input.GetKey(KeyCode.K))
            {
                CollectionSystem.CollectItem(CollectionSystem.CollectedType.Though, 100);
            }
        }
    }
}