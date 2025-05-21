namespace AbilitySystem
{
    public interface IAbility
    {
        void Activate();   // 當切換到這個 ability 時執行
        void Deactivate(); // 當切換離開這個 ability 時執行
        void Use();        // 當按下「使用能力」按鍵時執行
    }
}