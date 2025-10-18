using UnityEngine;

namespace EventBus.Events.Health
{
    public readonly struct OnHealthChanged : IEvent
    {
        public readonly GameObject target;
        public readonly int current;
        public readonly int max;
        public OnHealthChanged(GameObject target, int current, int max)
        { this.target = target; this.current = current; this.max = max; }
    }
}