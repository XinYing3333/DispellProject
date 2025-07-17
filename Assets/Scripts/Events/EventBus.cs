using System;
using System.Collections.Generic;

namespace Events
{
    /// <summary>
    /// 事件中心：集中管理各種觸發事件。
    /// </summary>
    public static class EventBus<T>
    {
        private static readonly List<Action<T>> Subscribers = new List<Action<T>>();

        public static void Subscribe(Action<T> callback)
        {
            if (!Subscribers.Contains(callback))
                Subscribers.Add(callback);
        }

        public static void Unsubscribe(Action<T> callback)
        {
            if (Subscribers.Contains(callback))
                Subscribers.Remove(callback);
        }

        public static void Publish(T publishedEvent)
        {
            foreach (var sub in Subscribers)
                sub.Invoke(publishedEvent);
        }
    }
}