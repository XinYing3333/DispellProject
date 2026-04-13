// using UnityEngine;
// using UnityEngine.SceneManagement;
//
// public class GameSaveManager : MonoBehaviour
// {
//     [SerializeField] private string defaultSlot = "slot1";
//     [SerializeField] private bool refreshOnSceneLoaded = true;
//
//     private void Awake()
//     {
//         if (refreshOnSceneLoaded)
//             SceneManager.sceneLoaded += HandleSceneLoaded;
//     }
//
//     private void OnDestroy()
//     {
//         if (refreshOnSceneLoaded)
//             SceneManager.sceneLoaded -= HandleSceneLoaded;
//     }
//
//     private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
//     {
//         RefreshPlacers();
//     }
//
//     // —— 讀檔（進遊戲時） ——
//     public void StartGameLoad()
//     {
//         // 念頭：用 LevelStateStore（有 saved/session）
//         var data = SaveSystem.Load(defaultSlot);
//         if (LevelStateStore.Instance != null)
//             LevelStateStore.Instance.ApplyFromSaveData(data);
//
//         // 物品：沿用你原本的 CollectionSystem API
//         CollectionSystem.LoadCollection();
//
//         RefreshPlacers();
//     }
//
//     // —— 存檔（玩家主動存） ——
//     public void RequestSave()
//     {
//         // 念頭：把 session 併入並寫回檔案
//         var data = SaveSystem.Load(defaultSlot) ?? new SaveData();
//         if (LevelStateStore.Instance != null)
//             LevelStateStore.Instance.WriteToSaveData(data);
//         SaveSystem.Save(defaultSlot, data);
//
//         // 物品：用舊 API 保存（沒有 session 的話就直接覆寫 JSON）
//         CollectionSystem.SaveCollection();
//
//         Debug.Log("[GameSaveManager] 存檔完成");
//     }
//
//     // —— 死亡/讀檔回檔（丟棄未存進度） ——
//     public void OnPlayerDeathOrReload()
//     {
//         // 念頭：丟棄未存的 session
//         if (LevelStateStore.Instance != null)
//             LevelStateStore.Instance.RevertToLastSave();
//
//         // 物品：因為你目前沒有 session 機制，直接重新 Load 回到上次保存狀態
//         CollectionSystem.LoadCollection();
//
//         RespawnPlayerAtCheckpoint();
//         RefreshPlacers();
//     }
//
//     // —— 新遊戲 —— 
//     public void NewGame()
//     {
//         // 念頭：清空並寫一個乾淨存檔
//         if (LevelStateStore.Instance != null)
//             LevelStateStore.Instance.ClearAll();
//         SaveSystem.Save(defaultSlot, new SaveData());
//
//         // 物品：清空（舊 API 會順便寫空檔）
//         CollectionSystem.ClearCollection();
//
//         RefreshPlacers();
//     }
//
//     private void RefreshPlacers()
//     {
//         var placers = FindObjectsOfType<ThoughtPlacer>(true);
//         foreach (var placer in placers)
//         {
//             if (Application.isPlaying)
//                 placer.Refresh(); // 請確保你已在 ThoughPlacer 加了這個 public 方法
//         }
//     }
//
//     private void RespawnPlayerAtCheckpoint()
//     {
//         // TODO：你的復活點邏輯
//     }
// }
