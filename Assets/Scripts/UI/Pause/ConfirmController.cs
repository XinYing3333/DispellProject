// ConfirmController.cs
namespace UI.Pause
{
    // Confirm 的最小邏輯：兩層結構（模式選擇 -> AreYouSure）
    public sealed class ConfirmController
    {
        public ConfirmContext Context { get; private set; } = ConfirmContext.QuitModeSelect;

        // 0/1 兩選一
        public int Selection { get; private set; } = 0;

        public QuitChoice Pending { get; private set; } = QuitChoice.ReturnToMainMenu;

        public void OpenQuit()
        {
            Context = ConfirmContext.QuitModeSelect;
            Selection = 0;
        }

        public void Navigate()
        {
            Selection = 1 - Selection;
        }

        // return: 是否仍在 Confirm
        public bool Submit(out bool confirmed, out QuitChoice choice)
        {
            confirmed = false;
            choice = Pending;

            if (Context == ConfirmContext.QuitModeSelect)
            {
                Pending = (Selection == 0) ? QuitChoice.ReturnToMainMenu : QuitChoice.ExitToDesktop;
                Context = (Pending == QuitChoice.ReturnToMainMenu) ? ConfirmContext.AreYouSureMain : ConfirmContext.AreYouSureExit;
                Selection = 0; // Yes/No
                return true;
            }

            // AreYouSure：Selection 0=Yes, 1=No
            if (Selection == 0)
            {
                confirmed = true;
                choice = Pending;
                return false; // Confirm 結束
            }

            // No：回到第一層
            Context = ConfirmContext.QuitModeSelect;
            Selection = 0;
            return true;
        }

        // return: 是否仍在 Confirm
        public bool Cancel()
        {
            // Cancel：AreYouSure 回 QuitModeSelect；QuitModeSelect 直接關閉 Confirm
            if (Context == ConfirmContext.QuitModeSelect)
                return false;

            Context = ConfirmContext.QuitModeSelect;
            Selection = 0;
            return true;
        }
    }
}