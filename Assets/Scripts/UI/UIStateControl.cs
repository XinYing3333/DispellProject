using System.Collections;
using EventBus.Events.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public enum UIState { Main, Menu, Gallery, Settings, Quit }

    public class UIStateControl : MonoBehaviour
    {
        private const string STATE_PARAM = "state";

        private UIState _currentUIState;

        [SerializeField] private Animator anim;

        [Header("Pause Root")]
        [SerializeField] private GameObject pauseRoot;
        public bool IsPauseOpen { get; private set; }
        
        [Header("Selection Watchdog")]
        [SerializeField] private float navInputThreshold = 0.2f; // 手把死區


        [Header("Main Buttons")]
        [SerializeField] private GameObject menuObj, galleryObj, settingsObj, quitObj, menuConfirmObj;
        [SerializeField] private Button menuBtn, galleryBtn, settingsBtn;

        [Header("Close Animation")]
        [SerializeField] private float quitAnimTime = 0.4f;
        [SerializeField] private string quitTrigger = "quit";

        private Coroutine _closeCo;

        private EventBinding<OnTogglePause> _binding;

        private void Start()
        {
            _currentUIState = UIState.Main;
            if (!anim) anim = GetComponent<Animator>();

            menuBtn = menuObj.GetComponent<Button>();
            galleryBtn = galleryObj.GetComponent<Button>();
            settingsBtn = settingsObj.GetComponent<Button>();

            if (pauseRoot) pauseRoot.SetActive(false);
        }

        private void OnEnable()
        {
            _binding = new EventBinding<OnTogglePause>(TogglePause);
            EventBus<OnTogglePause>.Register(_binding);
        }

        private void OnDisable()
        {
            if (_binding == null) return;
            EventBus<OnTogglePause>.Deregister(_binding);
            _binding = null;
        }
        
        private void Update()
        {
            WatchSelectionByNavigation();
        }

        private void WatchSelectionByNavigation()
        {
            if (!IsPauseOpen) return;
            if (_currentUIState == UIState.Quit) return;
            if (EventSystem.current == null) return;

            // 目前已選中且可用 -> 不動
            var cur = EventSystem.current.currentSelectedGameObject;
            if (cur != null && cur.activeInHierarchy) return;

            // 只有玩家真的在「導覽移動」才補焦點
            if (!HasNavigationInput()) return;

            EnsureSelection();
        }

        private bool HasNavigationInput()
        {
            // 新 Input System 開啟後，舊軸可能沒值；所以同時做鍵盤方向鍵/wasd 檢查
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > navInputThreshold || Mathf.Abs(v) > navInputThreshold)
                return true;

            // 鍵盤保底（WASD / 方向鍵）
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                return true;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.RightArrow))
                return true;

            return false;
        }

        
        public void TogglePause()
        {
            if (!IsPauseOpen) OpenPause();
            else SwitchCurrentUIState((int)UIState.Quit);
        }

        private void OpenPause()
        {
            if (_closeCo != null) { StopCoroutine(_closeCo); _closeCo = null; }

            IsPauseOpen = true;
            if (pauseRoot) pauseRoot.SetActive(true);
            if (anim) anim.ResetTrigger(quitTrigger);

            Time.timeScale = 0f;
            
            SwitchCurrentUIState((int)UIState.Main);
        }

        public void SwitchCurrentUIState(int state)
        {
            UIState newState = (UIState)state;

            // Quit 在關閉流程中可能會被重複呼叫，允許重入（不要 return）
            // if (_currentUIState == newState && newState != UIState.Quit)
            //     return;

            _currentUIState = newState;
            ApplyAnimatorState(_currentUIState);
            ApplyVisibility(_currentUIState);
            EnsureSelection();
        }

        private void ApplyAnimatorState(UIState s)
        {
            if (!anim) return;

            int v = s switch
            {
                UIState.Main => 0,
                UIState.Menu => 1,
                UIState.Gallery => 2,
                UIState.Settings => 3,
                UIState.Quit => 4,
                _ => 0
            };

            anim.SetInteger(STATE_PARAM, v);

            // ★ 立刻推進一次，避免你覺得「沒切到」
            anim.Update(0f);

            Debug.Log($"[UIStateControl] Set {STATE_PARAM}={v}, state={s}, animatorReadback={anim.GetInteger(STATE_PARAM)}");
        }

        private void ApplyVisibility(UIState s)
        {
            // Quit：只播動畫，不要在這裡把 root 關掉（會直接消失）
            if (s == UIState.Quit)
            {
                StartCloseSequence();
                return;
            }

            switch (s)
            {
                case UIState.Main:
                    menuObj.SetActive(true);
                    galleryObj.SetActive(true);
                    settingsObj.SetActive(true);
                    quitObj.SetActive(false);

                    menuBtn.interactable = true;
                    galleryBtn.interactable = true;
                    settingsBtn.interactable = true;
                    break;

                case UIState.Menu:
                case UIState.Gallery:
                case UIState.Settings:
                    menuObj.SetActive(false);
                    galleryObj.SetActive(false);
                    settingsObj.SetActive(false);
                    quitObj.SetActive(true);

                    menuBtn.interactable = false;
                    galleryBtn.interactable = false;
                    settingsBtn.interactable = false;
                    break;
            }
            Debug.Log($"[UIStateControl] Set {s}, state={s}");
        }

        private void StartCloseSequence()
        {
            if (_closeCo != null) return;

            if (EventSystem.current)
                EventSystem.current.SetSelectedGameObject(null);

            if (anim)
            {
                anim.ResetTrigger(quitTrigger);
                anim.SetTrigger(quitTrigger);
                anim.Update(0f);
            }

            _closeCo = StartCoroutine(CoCloseAfterAnim());
        }


        private IEnumerator CoCloseAfterAnim()
        {
            yield return new WaitForSecondsRealtime(quitAnimTime);

            Time.timeScale = 1f;
            if (pauseRoot) pauseRoot.SetActive(false);

            IsPauseOpen = false;
            _closeCo = null;

            _currentUIState = UIState.Main;
        }

        private void EnsureSelection()
        {
            if (EventSystem.current == null) return;
            if (_currentUIState == UIState.Quit) return;

            var cur = EventSystem.current.currentSelectedGameObject;
            if (cur && cur.activeInHierarchy) return;

            GameObject target = null;

            switch (_currentUIState)
            {
                case UIState.Main: target = menuObj; break;
                case UIState.Menu: target = menuConfirmObj; break;
                case UIState.Gallery: target = quitObj; break;
            }

            if (target != null)
                EventSystem.current.SetSelectedGameObject(target);
        }

        public void CmdClosePause()
        {
            if (!IsPauseOpen) return;
            if (!anim) return;
            SwitchCurrentUIState(0);
            anim.ResetTrigger(quitTrigger);
            anim.SetTrigger(quitTrigger);
            anim.Update(0f);
        }

        public void CmdBackToMenu()
        {
            Time.timeScale = 1f;
            SceneController.Instance.LoadScene("MainMenu");
        }
    }
}
