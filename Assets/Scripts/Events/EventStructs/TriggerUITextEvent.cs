namespace Events
{
    public struct TriggerUITextEvent
    {
        public string textToShow;
        public float displayTime;

        public TriggerUITextEvent(string text, float displayTime)
        {
            this.textToShow = text;
            this.displayTime = displayTime;
        }
    }
}