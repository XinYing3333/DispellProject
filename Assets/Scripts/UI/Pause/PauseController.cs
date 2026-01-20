// PauseController.cs
using System;
using UnityEngine;

namespace UI.Pause
{
    // 邏輯唯一入口：狀態機 + 輸入路由 + TimeScale。
    public sealed class PauseController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PauseView view;

        [Header("Pause")]
        [SerializeField] private bool useTimeScalePause = true;

        public PauseRootState State { get; private set; } = PauseRootState.Closed;

        public OptionsController Options { get; private set; } = new OptionsController();
        public ConfirmController Confirm { get; private set; } = new ConfirmController();

        public event Action RequestLoadMainMenu;
        public event Action RequestExitToDesktop;
        
        int mainIndex;
        public int MainIndex => mainIndex;
        
        bool prevSendNav;
        bool inputLocked;

        void Awake()
        {
            if (!view) view = FindFirstObjectByType<PauseView>(FindObjectsInactive.Include);

            if (view)
            {
                view.OpenFinished += OnOpenFinished;
                view.CloseFinished += OnCloseFinished;
                view.TabSwitchFinished += OnTabSwitchFinished;
                view.SetVisible(false);
            }
        }

        void OnDestroy()
        {
            if (!view) return;
            view.OpenFinished -= OnOpenFinished;
            view.CloseFinished -= OnCloseFinished;
            view.TabSwitchFinished -= OnTabSwitchFinished;
        }

        // ===== Input Commands (你用 InputSystem/按鈕/事件來呼叫這些) =====

        public void CmdPauseToggle()
        {
            // 在 Options/Confirm：等價 Cancel（依規格）
            if (State == PauseRootState.Options || State == PauseRootState.Confirm)
            {
                CmdCancel();
                return;
            }

            if (State == PauseRootState.Closed) StartOpen();
            else if (State == PauseRootState.MainMenu) StartClose();
        }

        public void CmdNavigate(Vector2 v)
        {
            if (inputLocked) return;

            int dx = v.x > 0.5f ? 1 : v.x < -0.5f ? -1 : 0;
            int dy = v.y > 0.5f ? 1 : v.y < -0.5f ? -1 : 0;
            if (dx == 0 && dy == 0) return;

            if (State == PauseRootState.MainMenu)
            {
                // MainMenu：上下移動選項（用 dy），左右也等價（避免不同設備差異）
                int step = dy != 0 ? -dy : dx; // 上=+1 => -1
                int count = 3; // 你若有第4顆就改 4，或做成可配置

                mainIndex = (mainIndex + step) % count;
                if (mainIndex < 0) mainIndex += count;

                view?.SetFocus($"Main/{mainIndex}");
                return;
            }

            if (State == PauseRootState.Options)
            {
                Options.Navigate(dx, dy);
                SyncOptionsFocusToView();
                return;
            }

            if (State == PauseRootState.Confirm)
            {
                Confirm.Navigate();
                SyncConfirmFocusToView();
                return;
            }
        }


        public void CmdTabLeft()
        {
            if (inputLocked) return;
            if (State != PauseRootState.Options) return;
            if (Options.IsEditing) return;

            var from = Options.Tab;
            Options.TabLeft();
            StartTabSwitch(from, Options.Tab);
        }

        public void CmdTabRight()
        {
            if (inputLocked) return;
            if (State != PauseRootState.Options) return;
            if (Options.IsEditing) return;

            var from = Options.Tab;
            Options.TabRight();
            StartTabSwitch(from, Options.Tab);
        }

        public void CmdSubmit()
        {
            if (inputLocked) return;

            if (State == PauseRootState.MainMenu)
            {
                // 0 Continue / 1 Options / 2 Quit（第4顆自己加 case）
                switch (mainIndex)
                {
                    case 0: Main_Continue(); break;
                    case 1: Main_Options(); break;
                    case 2: Main_Quit(); break;
                }
                return;
            }

            if (State == PauseRootState.Options)
            {
                Options.Submit();
                view?.SetEditing(Options.IsEditing, GetOptionsFieldId());
                SyncOptionsFocusToView();
                return;
            }

            if (State == PauseRootState.Confirm)
            {
                bool stillOpen = Confirm.Submit(out bool confirmed, out QuitChoice choice);
                if (!stillOpen)
                {
                    State = PauseRootState.MainMenu;
                    view?.ShowMain();
                    view?.SetFocus($"Main/{mainIndex}");
                }

                if (confirmed) DoQuit(choice);
                else SyncConfirmFocusToView();

                return;
            }
        }


        public void CmdCancel()
        {
            if (inputLocked) return;

            if (State == PauseRootState.Options)
            {
                if (Options.IsEditing)
                {
                    Options.Cancel(); // 退出 Editing
                    view?.SetEditing(false, GetOptionsFieldId());
                    SyncOptionsFocusToView();
                }
                else
                {
                    // Browsing：回 MainMenu（並套用 Draft）
                    SettingsStore.ApplyDraft();
                    State = PauseRootState.MainMenu;
                    view?.ShowMain();
                }
                return;
            }

            if (State == PauseRootState.Confirm)
            {
                bool stillOpen = Confirm.Cancel();
                if (!stillOpen)
                {
                    State = PauseRootState.MainMenu;
                    view?.ShowMain();
                }
                else
                {
                    SyncConfirmFocusToView();
                }
                return;
            }

            if (State == PauseRootState.MainMenu)
            {
                StartClose();
            }
        }

        // ===== Main Menu Buttons (直接綁 TMP Button OnClick) =====

        public void Main_Continue()
        {
            if (State != PauseRootState.MainMenu) return;
            StartClose();
        }

        public void Main_Options()
        {
            if (State != PauseRootState.MainMenu) return;

            SettingsStore.BeginDraft();
            Options.EnterDefault();
            State = PauseRootState.Options;

            view?.ShowOptions();
            view?.SetEditing(false, GetOptionsFieldId());
            SyncOptionsFocusToView();
        }

        public void Main_Quit()
        {
            if (State != PauseRootState.MainMenu) return;

            Confirm.OpenQuit();
            State = PauseRootState.Confirm;

            view?.ShowConfirm();
            SyncConfirmFocusToView();
        }

        // ===== Internal State Machine =====

        void StartOpen()
        {
            if (State != PauseRootState.Closed) return;

            State = PauseRootState.Opening;
            inputLocked = true;

            ApplyPauseOn();
            view?.PlayOpen();
            if (view == null) OnOpenFinished();
        }

        void OnOpenFinished()
        {
            if (State != PauseRootState.Opening) return;

            State = PauseRootState.MainMenu;
            inputLocked = false;

            mainIndex = 0;
            view?.ShowMain();
            view?.SetFocus($"Main/{mainIndex}");
        }


        void StartClose()
        {
            if (State == PauseRootState.Closed || State == PauseRootState.Closing) return;

            State = PauseRootState.Closing;
            inputLocked = true;

            view?.PlayClose();
            if (view == null) OnCloseFinished();
        }

        void OnCloseFinished()
        {
            if (State != PauseRootState.Closing) return;

            ApplyPauseOff();

            State = PauseRootState.Closed;
            inputLocked = false;
        }

        void StartTabSwitch(OptionsTab from, OptionsTab to)
        {
            inputLocked = true;
            view?.PlayTabSwitch(from, to);
            if (view == null) OnTabSwitchFinished();
        }

        void OnTabSwitchFinished()
        {
            inputLocked = false;
            SyncOptionsFocusToView();
        }

        void ApplyPauseOn()
        {
            if (useTimeScalePause) Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es)
            {
                prevSendNav = es.sendNavigationEvents;
                es.sendNavigationEvents = false; // 重要：關掉 Unity 自動 navigation
            }
        }

        void ApplyPauseOff()
        {
            if (useTimeScalePause) Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es)
                es.sendNavigationEvents = prevSendNav; // 還原
        }


        void DoQuit(QuitChoice choice)
        {
            // 退出前把 pause 狀態還原，避免 timeScale 卡 0
            ApplyPauseOff();
            State = PauseRootState.Closed;

            if (choice == QuitChoice.ReturnToMainMenu)
            {
                RequestLoadMainMenu?.Invoke();
            }
            else
            {
                RequestExitToDesktop?.Invoke();
#if UNITY_EDITOR
                Debug.Log("[Pause] ExitToDesktop (Editor ignored).");
#else
                Application.Quit();
#endif
            }
        }

        // ===== Focus/Editing IDs (給 View 用) =====

        void SyncOptionsFocusToView()
        {
            view?.SetFocus(GetOptionsFieldId());
        }

        string GetOptionsFieldId()
        {
            // 例：Options/Audio/0
            return $"Options/{Options.Tab}/{Options.CurrentFieldIndex}";
        }

        void SyncConfirmFocusToView()
        {
            // 例：Confirm/QuitModeSelect/0
            view?.SetFocus($"Confirm/{Confirm.Context}/{Confirm.Selection}");
        }

        void OnDisable()
        {
            // 保險：不留 timeScale=0
            if (useTimeScalePause) Time.timeScale = 1f;
        }
    }
}
