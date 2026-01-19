using System.Collections;
using Player;
using Player.InteractionSystem;
using UnityEngine;

namespace DialogSystem
{
    public class DialogueTrigger : MonoBehaviour, IInteractable, IFocusable
    {
        [Header("KeyE")] [SerializeField] private GameObject keyE;

        /*[Header("SceneSwitcher")]
    [SerializeField] private SceneSwitcher sceneSwitcher;*/
        [Tooltip("角色模型動畫")]
        [Header("Emote Animator")] [SerializeField]
        private Animator emoteAnimator;

        [Header("Ink JSON")] [SerializeField] private TextAsset inkJSON;

        [Header("互動設定")] public string prompt = "按 E 對話";
        [Tooltip("是否只播放一次")]
        public bool oneShot = false;
        [Tooltip("對話冷卻")]
        public float cooldown = 0.5f;
        [Tooltip("是否自動播放對話")]
        public bool autoDisplay = false;
        public bool lockMovement = true;
    
        [Header("觸發模式")]
        [SerializeField]private TriggerMode mode = TriggerMode.InteractPress;

        private enum TriggerMode
        {
            InteractPress,   // 需要按 E
            AutoOnEnter,     // 進入碰撞區自動
            AutoOnStart,     // ★ 場景開始時自動
            ExternalOnly     // 外部呼叫
        }

        float _lastTime = -999f;
        bool _consumed;

        public string Prompt => prompt;

        public void OnFocusGained()
        {
            if (mode != TriggerMode.InteractPress) return;
            if (keyE) keyE.SetActive(true);
        }

        public void OnFocusLost()
        {
            if (mode != TriggerMode.InteractPress) return;
            if (keyE) keyE.SetActive(false);
        }

        public void Interact()
        {
            if (_consumed) return;
            if (Time.time - _lastTime < cooldown) return;
            if (DialogueManager.GetInstance().dialogueIsPlaying) return;

            _lastTime = Time.time;

            DialogueManager.GetInstance().EnterDialogueMode(inkJSON, emoteAnimator,autoDisplay,lockMovement);

            //GameEvents.DialogueStarted?.Invoke();
            if (oneShot) _consumed = true;
        }

        void OnDialogueEnd()
        {
            //GameEvents.DialogueEnded?.Invoke();
        }

        void Awake()
        {
            keyE.SetActive(false);
            emoteAnimator = transform.GetChild(0).GetComponent<Animator>();
        }
    
        private void Start()
        {
            // ★ 場景一開始自動播放對話
            if (mode == TriggerMode.AutoOnStart)
            {
                // 為避免其他系統（相機/玩家初始化）尚未完成，可稍微延遲一幀
                StartCoroutine(PlayDialogueAfterFrame());
            }
        }

        private IEnumerator PlayDialogueAfterFrame()
        {
            yield return null; // 等待 1 frame
            Interact();
        }
    
        private void OnCollisionEnter(Collision collision)
        {
            if (mode != TriggerMode.AutoOnEnter) return;
            if (!collision.gameObject.CompareTag("Player")) return;
            Interact(); // 直接觸發
        }

// 劇情/任務系統呼叫
        public void TriggerExternally()
        {
            if (mode == TriggerMode.ExternalOnly) Interact();
        }
    }
}