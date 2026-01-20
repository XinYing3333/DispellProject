// PauseTestDriver.cs
// 測試用：不依賴你的 Input System，直接用鍵盤把 6 個命令打通。
// 掛在任意物件上即可（建議掛在 PauseController 同物件）。

using UnityEngine;

namespace UI.Pause
{
    public sealed class PauseTestDriver : MonoBehaviour
    {
        [SerializeField] private PauseController controller;

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
            if (!controller) return;

            // PauseToggle（在 Options/Confirm 時等價於 Cancel）
            if (Input.GetKeyDown(pauseToggle))
                controller.CmdPauseToggle();

            // Submit / Cancel
            if (Input.GetKeyDown(submit))
                controller.CmdSubmit();

            if (Input.GetKeyDown(cancel))
                controller.CmdCancel();

            // Tab（只在 Options & Browsing 生效）
            if (Input.GetKeyDown(tabLeft))
                controller.CmdTabLeft();

            if (Input.GetKeyDown(tabRight))
                controller.CmdTabRight();

            // Navigate：用「本幀首次按下」組合成 Vector2
            Vector2 nav = Vector2.zero;

            if (Input.GetKeyDown(left)) nav.x = -1f;
            else if (Input.GetKeyDown(right)) nav.x = 1f;

            if (Input.GetKeyDown(up)) nav.y = 1f;
            else if (Input.GetKeyDown(down)) nav.y = -1f;

            if (nav != Vector2.zero)
                controller.CmdNavigate(nav);

            // Debug print
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
