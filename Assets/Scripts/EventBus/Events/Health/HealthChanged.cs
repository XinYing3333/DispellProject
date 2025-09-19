using UnityEngine;

namespace EventBus.Events.Health
{
    public readonly struct HealthChanged : IEvent
    {
        public readonly GameObject target;
        public readonly int current;
        public readonly int max;
        public HealthChanged(GameObject target, int current, int max)
        { this.target = target; this.current = current; this.max = max; }
    }
}