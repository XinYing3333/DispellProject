using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneDirector : MonoBehaviour
{
    [Header("Settings")]
    public Transform defaultSpawnPoint;
    public GameObject[] tutorialObjects; // 把需要根據進度關閉的教學物件放這

    [SerializeField] private GameObject pangolinIdle, pangolinFollow1, pangolinFollow2, collectPanel;
    [SerializeField] private GameObject totemDoor;

    private void Start()
    {
        StartCoroutine(InitSceneRoutine());
        
    }

    private IEnumerator InitSceneRoutine()
    {
        // 1. 等待一幀確保 DataManager 和 CheckpointManager 註冊完成
        yield return null;

        // 2. 處理玩家生成
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            var respawn = player.GetComponent<RespawnController>();
            var data = DataManager.Instance.gameData;

            if (data.lastSceneName == SceneManager.GetActiveScene().name)
            {
                var cp = CheckpointManager.Instance.GetCheckpointById(data.lastCheckpointId);
                if (cp != null) {
                    respawn.PlaceAt(cp.GetSpawnTransform().position, cp.GetSpawnTransform().rotation);
                } else {
                    respawn.PlaceAt(data.fallbackPos, Quaternion.Euler(data.fallbackEuler));
                }
            }
            else if (defaultSpawnPoint) {
                respawn.PlaceAt(defaultSpawnPoint.position, defaultSpawnPoint.rotation);
            }
        }

        // 3. 處理教學與場景物件狀態 (核心邏輯)
        foreach (var obj in tutorialObjects)
        {
            // 假設物件名字就是 ID，或者你可以寫個組件來定義 ID
            if (DataManager.Instance.IsIdTriggered(obj.name)) {
                obj.SetActive(false); 
            }
        }
        
        if (DataManager.Instance.gameData.isFirstAdsorbDone)
        {
            pangolinIdle.SetActive(false);
            pangolinFollow1.SetActive(true);
            pangolinFollow2.SetActive(true);
            collectPanel.SetActive(true);
        }
        if (DataManager.Instance.gameData.isTotemDoorDone)
        {
            totemDoor.SetActive(false);
            AudioManager.Instance.PlayBGM(BGMType.None);
        }

        // 4. 最後才淡入畫面 (可選)
        // ScreenFader.FadeIn();
    }
}