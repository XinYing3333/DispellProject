namespace DefaultNamespace.EventBus.Events.UI
{
    // 1) 任務內容更新（任務系統發）
    //    改成：Key + Args（不再直接傳顯示字串）
    public readonly struct SetObjective : IEvent
    {
        public readonly string   Key;
        public readonly object[] Args;

        public SetObjective(string key, object[] args = null)
        {
            Key  = key;
            Args = args;
        }
    }

    // 2) 玩家要求顯示（輸入系統發）
    public readonly struct RevealObjective : IEvent { }

    // 3) 語言切換（全域只 Raise 一次）
    public readonly struct LanguageChanged : IEvent
    {
        public readonly UnityEngine.SystemLanguage Language;
        public LanguageChanged(UnityEngine.SystemLanguage language) => Language = language;
    }
}