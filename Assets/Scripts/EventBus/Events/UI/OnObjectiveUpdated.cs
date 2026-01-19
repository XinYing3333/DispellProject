namespace DefaultNamespace.EventBus.Events.UI
{
    // 1) 任務內容更新（任務系統發）
    public readonly struct SetObjective : IEvent
    {
        public readonly string Text;
        public SetObjective(string text) => Text = text;
    }

    // 2) 玩家要求顯示（輸入系統發）
    public readonly struct RevealObjective : IEvent {}

    // 3) 玩家要求隱藏（輸入系統發）
    //public readonly struct HideObjective : IEvent {}
}