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

    public DebugMethodAttribute(string button_label, string category = "")
    {
        ButtonLabel = button_label;
        Category = category;
    }
}

/// <summary>
/// 标记调试工具中需要自动注册到面板的变量（变量名即显示名称和键值）
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class DebugPropertyAttribute : Attribute
{
}