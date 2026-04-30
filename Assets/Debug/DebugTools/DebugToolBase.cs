using UnityEngine;

/// <summary>
/// 调试工具基类 - 所有自定义调试操作工具类的基类，提供对DebugPanel的便捷访问
/// </summary>
public abstract class DebugToolBase
{
    protected DebugPanel Panel => DebugPanel.Instance;
}