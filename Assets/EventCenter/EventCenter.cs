using System;
using System.Runtime.CompilerServices;

public static class EventCenter
{
    // 预分配雷霆大的数组，避免动态扩容
    private const int MAX_EVENTS = 1024;
    private static int next_id_;
    private static readonly Delegate[] events_ = new Delegate[MAX_EVENTS];
    public static int AllocateId()
    {
#if DEBUG
        if (next_id_ >= MAX_EVENTS)
            throw new InvalidOperationException(
                $"EventCenter: ID overflow. Current: {next_id_}, Max: {MAX_EVENTS}. " +
                "Increase MAX_EVENTS or check for leaks.");
#endif
        return next_id_++;
    }


    public interface IEventKey
    {
        int Index { get; }
    }

    // 用显式ID构造, 使用结构体体积小，可以不用new，直接声明即可
    public readonly struct EventKey : IEventKey
    {
        private readonly int index_;
        public int Index => index_;
        public EventKey(int _ = -1) => index_ = AllocateId();
    }
    public readonly struct EventKey<T> : IEventKey
    {
        private readonly int index_;
        public int Index => index_;
        public EventKey(int _ = -1) => index_ = AllocateId();
    }
    public readonly struct EventKey<T1, T2> : IEventKey
    {
        private readonly int index_;
        public int Index => index_;
        public EventKey(int _ = -1) => index_ = AllocateId();
    }
    public readonly struct EventKey<T1, T2, T3> : IEventKey
    {
        private readonly int index_;
        public int Index => index_;
        public EventKey(int _ = -1) => index_ = AllocateId();
    }

    #region 订阅
    public static void Subscribe(this object _, EventKey key, Action handler)
        => AddHandler(key.Index, handler);
    public static void Subscribe<T>(this object _, EventKey<T> key, Action<T> handler)
        => AddHandler(key.Index, handler);
    public static void Subscribe<T1, T2>(this object _, EventKey<T1, T2> key, Action<T1, T2> handler)
        => AddHandler(key.Index, handler);
    public static void Subscribe<T1, T2, T3>(this object _, EventKey<T1, T2, T3> key, Action<T1, T2, T3> handler)
        => AddHandler(key.Index, handler);
    #endregion

    #region 取消订阅
    public static void Unsubscribe(this object _, EventKey key, Action handler)
        => RemoveHandler(key.Index, handler);
    public static void Unsubscribe<T>(this object _, EventKey<T> key, Action<T> handler)
        => RemoveHandler(key.Index, handler);
    public static void Unsubscribe<T1, T2>(this object _, EventKey<T1, T2> key, Action<T1, T2> handler)
        => RemoveHandler(key.Index, handler);
    public static void Unsubscribe<T1, T2, T3>(this object _, EventKey<T1, T2, T3> key, Action<T1, T2, T3> handler)
        => RemoveHandler(key.Index, handler);
    #endregion

    #region 触发
    public static void Publish(this object _, EventKey key)
    {
        var cb = Unsafe.As<Action>(events_[key.Index]);         // 这里的cast非常不安全，但为了性能我们假设调用者都能正确使用
        cb?.Invoke();
    }
    public static void Publish<T>(this object _, EventKey<T> key, T arg)
    {
        var cb = Unsafe.As<Action<T>>(events_[key.Index]);
        cb?.Invoke(arg);
    }
    public static void Publish<T1, T2>(this object _, EventKey<T1, T2> key, T1 arg1, T2 arg2)
    {
        var cb = Unsafe.As<Action<T1, T2>>(events_[key.Index]);
        cb?.Invoke(arg1, arg2);
    }
    public static void Publish<T1, T2, T3>(this object _, EventKey<T1, T2, T3> key, T1 arg1, T2 arg2, T3 arg3)
    {
        var cb = Unsafe.As<Action<T1, T2, T3>>(events_[key.Index]);
        cb?.Invoke(arg1, arg2, arg3);
    }
    #endregion

    #region 辅助
    private static void AddHandler(int index, Delegate handler)
    {
        events_[index] = Delegate.Combine(events_[index], handler);
    }
    private static void RemoveHandler(int index, Delegate handler)
    {
        events_[index] = Delegate.Remove(events_[index], handler);
    }
    #endregion
}