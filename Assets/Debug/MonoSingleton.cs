using UnityEngine;

/// <summary>
/// 泛型单例基类（继承MonoBehaviour）
/// </summary>
public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T                      instance_;

    public static T Instance
    {
        get
        {
            // 已初始化直接返回
            if (instance_ != null) return instance_;

            // 尝试查找场景中的现有实例
            instance_ = FindAnyObjectByType<T>();
            if (instance_ != null) return instance_;

            // 创建带有类型标记的新游戏对象
            GameObject go = new GameObject(typeof(T).Name);
            instance_ = go.AddComponent<T>();
            instance_.transform.position = Vector3.zero;
            return instance_;
        }
    }

    protected virtual void Awake()
    {
        transform.position = Vector3.zero;
        if (instance_ == null)
        {
            instance_ = this as T;
        }
        else if (instance_ != this)
        {
            Destroy(instance_.gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (instance_ == this) instance_ = null;
    }
}
