// PauseEnums.cs
namespace UI.Pause
{
    public enum PauseRootState
    {
        Closed,
        Opening,
        MainMenu,
        Options,
        Confirm,
        Closing
    }

    public enum OptionsTab
    {
        Audio,
        Video,
        Controls
    }

    public enum OptionsSubState
    {
        Browsing,
        Editing
    }

    public enum ConfirmContext
    {
        QuitModeSelect,   // 回主選單 / 退出桌面
        AreYouSureMain,   // 確認回主選單
        AreYouSureExit    // 確認退出桌面
    }

    public enum QuitChoice
    {
        ReturnToMainMenu,
        ExitToDesktop
    }
}