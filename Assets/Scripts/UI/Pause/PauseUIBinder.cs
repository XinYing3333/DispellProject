// PauseUIBinder.cs
// 加入：MainPanel 的文字提示（顯示目前 mainIndex 選到哪個）
// 需求：PauseController 需暴露目前 MainMenu 選擇索引（MainIndex）。

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Pause
{
    public sealed class PauseUIBinder : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PauseController controller;

        [Header("Root Panels")]
        [SerializeField] private GameObject root;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject confirmPanel;

        [Header("Main Text Hint")]
        [SerializeField] private TextMeshProUGUI mainHeader;
        [SerializeField] private TextMeshProUGUI mainHint; // 顯示「▶ Continue / ▶ Options / ▶ Quit」

        [Header("Options Texts")]
        [SerializeField] private TextMeshProUGUI optionsHeader;
        [SerializeField] private TextMeshProUGUI[] optionLines = new TextMeshProUGUI[3];

        [Header("Confirm Texts")]
        [SerializeField] private TextMeshProUGUI confirmHeader;
        [SerializeField] private TextMeshProUGUI confirmBody;
        [SerializeField] private TextMeshProUGUI confirmLeft;
        [SerializeField] private TextMeshProUGUI confirmRight;

        void Awake()
        {
            if (!controller) controller = FindFirstObjectByType<PauseController>();
        }

        void Update()
        {
            if (!controller) return;

            bool visible = controller.State != PauseRootState.Closed;
            if (root && root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;

            if (mainPanel) mainPanel.SetActive(controller.State == PauseRootState.MainMenu);
            if (optionsPanel) optionsPanel.SetActive(controller.State == PauseRootState.Options);
            if (confirmPanel) confirmPanel.SetActive(controller.State == PauseRootState.Confirm);

            if (controller.State == PauseRootState.MainMenu) RenderMain();
            if (controller.State == PauseRootState.Options) RenderOptions();
            if (controller.State == PauseRootState.Confirm) RenderConfirm();
        }

        void RenderMain()
        {
            if (mainHeader)
                mainHeader.text = "Pause Menu";

            if (!mainHint) return;

            // 需要 PauseController 提供 MainIndex
            int idx = controller.MainIndex;

            string l0 = (idx == 0 ? "▶ " : "  ") + "Continue";
            string l1 = (idx == 1 ? "▶ " : "  ") + "Options";
            string l2 = (idx == 2 ? "▶ " : "  ") + "Quit";

            mainHint.text = $"{l0}\n{l1}\n{l2}\n\n" +
                            "Navigate：↑↓（或←→）\nSubmit：Space\nCancel：Backspace";
        }

        void RenderOptions()
        {
            var tab = controller.Options.Tab;
            var sub = controller.Options.SubState;
            int idx = controller.Options.CurrentFieldIndex;

            if (optionsHeader)
                optionsHeader.text = $"Options | {tab} | {sub} | Field {idx}";

            var s = SettingsStore.Draft;

            string a0, a1, a2;
            if (tab == OptionsTab.Audio)
            {
                a0 = $"Master : {s.audio.master}";
                a1 = $"Music  : {s.audio.music}";
                a2 = $"SFX    : {s.audio.sfx}";
            }
            else if (tab == OptionsTab.Video)
            {
                var res = OptionsData.Resolutions[Mathf.Clamp(s.video.resolutionIndex, 0, OptionsData.Resolutions.Count - 1)];
                var asp = OptionsData.AspectRatios[Mathf.Clamp(s.video.aspectIndex, 0, OptionsData.AspectRatios.Count - 1)];
                a0 = $"Resolution : {res.x}x{res.y}";
                a1 = $"Aspect     : {asp}";
                a2 = $"Fullscreen : {(s.video.fullscreen ? "On" : "Off")}";
            }
            else
            {
                a0 = $"Invert Camera : {(s.controls.invertCamera ? "On" : "Off")}";
                a1 = $"Sensitivity   : {s.controls.sensitivity}";
                a2 = "(reserved)";
            }

            string[] lines = { a0, a1, a2 };

            for (int i = 0; i < optionLines.Length && i < 3; i++)
            {
                if (!optionLines[i]) continue;
                bool selected = (i == idx);
                bool editing = controller.Options.IsEditing && selected;
                string prefix = editing ? "▶▶ " : selected ? "▶ " : "  ";
                optionLines[i].text = prefix + lines[i];
            }
        }

        void RenderConfirm()
        {
            if (confirmHeader)
                confirmHeader.text = $"Confirm | {controller.Confirm.Context}";

            string body;
            string left;
            string right;

            if (controller.Confirm.Context == ConfirmContext.QuitModeSelect)
            {
                body = "Quit：選擇行為";
                left = "回主選單";
                right = "退出桌面";
            }
            else if (controller.Confirm.Context == ConfirmContext.AreYouSureMain)
            {
                body = "確定回到主選單？";
                left = "Yes";
                right = "No";
            }
            else
            {
                body = "確定退出至桌面？";
                left = "Yes";
                right = "No";
            }

            if (confirmBody) confirmBody.text = body;

            int sel = controller.Confirm.Selection;
            if (confirmLeft)  confirmLeft.text  = (sel == 0 ? "▶ " : "  ") + left;
            if (confirmRight) confirmRight.text = (sel == 1 ? "▶ " : "  ") + right;
        }
    }
}
