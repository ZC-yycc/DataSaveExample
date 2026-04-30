using System;

/// <summary>
/// 标记调试面板中自定义操作方法的特性
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DebugMethodAttribute : Attribute
{
    /// <summary>
    /// 按钮显示文本
    /// </summary>
    public string ButtonLabel { get; }

    /// <summary>
    /// 分类名称（可选，用于分组显示）
    /// </summary>
    public string Category { get; set; }

    public DebugMethodAttribute(string button_label)
    {
        ButtonLabel = button_label;
    }
}