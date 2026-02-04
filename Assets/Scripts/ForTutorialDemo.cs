using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class ForTutorialDemo : MonoBehaviour
    {
        // --- Stage 狀態：0 -> 第一關卡；1 -> 第二關卡；>=2 -> 完成 ---
        [SerializeField] private int stage = 0;

        // ---- 第一階段（吸 5 個 Though）----
        [Header("Stage 0: Collect Though")]
        [SerializeField, Min(1)] private int requiredThough = 5;
        [SerializeField] private bool useDeltaForStage0 = true; // 避免舊存檔秒解
        private int baselineThough;
        [SerializeField] private LevelSequenceTrigger enemyTutorial ; 
        [SerializeField] private GameObject collectionPanel ; 

        // ---- 第二階段（收集特定 Offering）----
        [Header("Stage 1: Collect Offering")]
        [Tooltip("若你要判斷『特定種類』，請在註解位置改成你的判斷邏輯")]
        [SerializeField] private bool anyOfferingOk = true; // true = 任何 Offering；false = 你自己在程式內改成特定種類檢查
        [SerializeField] private LevelSequenceTrigger finishTutorial ; 
        
        public static bool isTutorialFinished = false;
        public GameObject pangolinIdle;
        public GameObject pangolin1;
        public GameObject pangolin2;
        
        private void OnEnable()
        {
            if (isTutorialFinished)
            {
                pangolinIdle.gameObject.SetActive(false);
                pangolin1.gameObject.SetActive(true);
                pangolin2.gameObject.SetActive(true);
                collectionPanel.SetActive(true);
                return;
            }
            if (stage == 0 && useDeltaForStage0)
                baselineThough = CollectionSystem.GetItemCount(CollectionSystem.CollectedType.Though);
            if (stage == 2)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (isTutorialFinished)
            {
                pangolinIdle.gameObject.SetActive(false);
                pangolin1.gameObject.SetActive(true);
                pangolin2.gameObject.SetActive(true);
                collectionPanel.SetActive(true);
                return;
            }
        }

        private void Update()
        {
            if(isTutorialFinished)return;
            switch (stage)
            {
                case 0:
                {
                    int shown = CollectionSystem.GetItemCount(CollectionSystem.CollectedType.Though);
                    int progress = useDeltaForStage0 ? Mathf.Max(0, shown - baselineThough) : shown;

                    if (progress >= requiredThough)
                    {
                        enemyTutorial.Play();
                        stage = 1;

                        // 進入下一段前若你也想用 Delta，可在此建立新的 baseline（若第二段也需要）
                        // baselineXxx = ...
                    }
                    break;
                }

                case 1:
                {
                    bool done;
                    if (anyOfferingOk)
                    {
                        // 只要曾收集過 Offering 就算
                        done = CollectionSystem.HasCollected(CollectionSystem.CollectedType.EnemyThough);
                    }
                    else
                    {
                        // TODO: 在這裡換成「特定種類」的判斷（例如查你的背包/ID/標籤）
                        // done = YourInventory.Has("Offering_SpecificId");
                        done = false;
                    }

                    if (done)
                    {
                        finishTutorial.Play();
                        isTutorialFinished = true;
                        Debug.Log("tutorial finished");
                        // TODO: 第二階段完成時要做的事（播教學B / 開UI / 等等）
                        stage = 2; // 全部完成
                    }
                    break;
                }

                default:
                    // 已全部完成（如需循環或重置，自己加）
                    break;
            }
        }
    }
}
