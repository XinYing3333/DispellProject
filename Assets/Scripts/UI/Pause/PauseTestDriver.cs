// PauseTestDriver.cs
// 測試用：不依賴 Input System，直接用鍵盤把 6 個命令打通。
// 正式接 PlayerInput 後：把 enableTestDriver 關掉，避免雙路徑重複觸發。

using UnityEngine;

namespace UI.Pause
{
    public sealed class PauseTestDriver : MonoBehaviour
    {
        [SerializeField] private PauseController controller;

        [Header("Enable")]
        [SerializeField] private bool enableTestDriver = false;

        [Header("Key Mapping")]
        [SerializeField] private KeyCode pauseToggle = KeyCode.Escape;
        [SerializeField] private KeyCode submit = KeyCode.Space;
        [SerializeField] private KeyCode cancel = KeyCode.Backspace;

        [SerializeField] private KeyCode tabLeft = KeyCode.Q;
        [SerializeField] private KeyCode tabRight = KeyCode.E;

        [SerializeField] private KeyCode up = KeyCode.W;
        [SerializeField] private KeyCode down = KeyCode.S;
        [SerializeField] private KeyCode left = KeyCode.A;
        [SerializeField] private KeyCode right = KeyCode.D;

        [Header("Debug Print")]
        [SerializeField] private KeyCode printState = KeyCode.P;

        void Awake()
        {
            if (!controller) controller = FindFirstObjectByType<PauseController>();
        }

        void Update()
        {
            if (!enableTestDriver) return;
            if (!controller) return;

            if (Input.GetKeyDown(pauseToggle))
                controller.CmdPauseToggle();

            if (Input.GetKeyDown(submit))
                controller.CmdSubmit();

            if (Input.GetKeyDown(cancel))
                controller.CmdCancel();

            if (Input.GetKeyDown(tabLeft))
                controller.CmdTabLeft();

            if (Input.GetKeyDown(tabRight))
                controller.CmdTabRight();

            Vector2 nav = Vector2.zero;

            if (Input.GetKeyDown(left)) nav.x = -1f;
            else if (Input.GetKeyDown(right)) nav.x = 1f;

            if (Input.GetKeyDown(up)) nav.y = 1f;
            else if (Input.GetKeyDown(down)) nav.y = -1f;

            if (nav != Vector2.zero)
                controller.CmdNavigate(nav);

            if (Input.GetKeyDown(printState))
            {
                Debug.Log(
                    $"[PauseTest] State={controller.State} " +
                    $"| Tab={controller.Options.Tab} Sub={controller.Options.SubState} Field={controller.Options.CurrentFieldIndex} " +
                    $"| Confirm={controller.Confirm.Context} Sel={controller.Confirm.Selection}"
                );

                var d = SettingsStore.Draft;
                Debug.Log(
                    $"[Draft] Audio {d.audio.master}/{d.audio.music}/{d.audio.sfx} " +
                    $"| Video ResIdx={d.video.resolutionIndex} AspIdx={d.video.aspectIndex} Fs={d.video.fullscreen} " +
                    $"| Ctrl Invert={d.controls.invertCamera} Sens={d.controls.sensitivity}"
                );
            }
        }
    }
}
