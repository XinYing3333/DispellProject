namespace EventBus.Events.Collect
{
    public readonly struct OnDoorCollectStarted : IEvent
    {
        public readonly int required;
        public OnDoorCollectStarted(int required) { this.required = required;}
    }
}