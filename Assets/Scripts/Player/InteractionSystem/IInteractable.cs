namespace DialogSystem
{
    public interface IInteractable
    {
        string Prompt { get; }     // UI 提示字串
        void Interact();           // 執行互動
    }
}