namespace EventBus
{
    public interface IEventBus { }
    public interface IEventBus<T1> { }
    public interface IEventBus<T1, T2> { }
    public interface IEventBus<T1, T2, T3> { }

    public static class EventBus<EventType> where EventType : IEventBus
    {
        public delegate void EventHandler();
        public static event EventHandler OnEvent;
        public static void Publish()
        {
            OnEvent?.Invoke();
        }
    }

    public static class EventBus<EventType, T1> where EventType : IEventBus<T1>
    {
        public delegate void EventHandler(T1 arg1);
        public static event EventHandler OnEvent;
        public static void Publish(in T1 arg1)
        {
            OnEvent?.Invoke(arg1);
        }
    }

    public static class EventBus<EventType, T1, T2> where EventType : IEventBus<T1, T2>
    {
        public delegate void EventHandler(T1 arg1, T2 arg2);
        public static event EventHandler OnEvent;
        public static void Publish(in T1 arg1, in T2 arg2)
        {
            OnEvent?.Invoke(arg1, arg2);
        }
    }

    public static class EventBus<EventType, T1, T2, T3> where EventType : IEventBus<T1, T2, T3>
    {
        public delegate void EventHandler(T1 arg1, T2 arg2, T3 arg3);
        public static event EventHandler OnEvent;
        public static void Publish(in T1 arg1, in T2 arg2, in T3 arg3)
        {
            OnEvent?.Invoke(arg1, arg2, arg3);
        }
    }
}