using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 调试页面管理器 - 提供运行时调试界面
/// </summary>
public class DebugPanel : MonoSingleton<DebugPanel>
{
    [Header("显示设置")]
    [SerializeField] private KeyCode                toggle_key_ = KeyCode.F1;
    [SerializeField] private bool                   displayed_ = false;
    [SerializeField] private int                    font_size_ = 14;

    private Vector2                                 scroll_position_ = Vector2.zero;
    private int                                     selected_tab_ = 0;
    private readonly string[]                       tab_names_ = { "性能", "日志", "系统信息", "游戏变量", "快捷操作" };

    // 性能监控
    private float                                   delta_time_ = 0.0f;
    private float                                   fps_ = 0;
    private float                                   memory_usage_ = 0;
    private int                                     draw_calls_ = 0;

    // 日志系统
    private readonly List<LogEntry>                 log_entries_ = new List<LogEntry>();
    private string                                  log_filter_ = "";
    private bool                                    show_info_logs_ = true;
    private bool                                    show_warning_ogs_ = true;
    private bool                                    show_error_logs_ = true;

    // 游戏变量存储
    private readonly Dictionary<string, object>     game_variables_ = new Dictionary<string, object>();
    private string                                  new_var_key_ = "";
    private string                                  new_var_value_ = "";
    private int                                     selected_var_type_ = 0;
    private readonly string[]                       var_types_ = { "int", "float", "string", "bool" };

    private void Start()
    {
        Application.logMessageReceived += HandleLog;

        // 添加默认变量
        AddGameVariable("PlayerHealth", 100);
        AddGameVariable("PlayerScore", 0);
        AddGameVariable("Level", 1);
        AddGameVariable("GameVersion", Application.version);
    }

    protected override void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        // 切换调试面板
        if (Input.GetKeyDown(toggle_key_))
        {
            displayed_ = !displayed_;
        }

        // 更新性能数据
        delta_time_ += (Time.unscaledDeltaTime - delta_time_) * 0.1f;
        fps_ = 1.0f / delta_time_;
        memory_usage_ = System.GC.GetTotalMemory(false) / (1024 * 1024f);
    }

    private void OnGUI()
    {
        if (!displayed_) return;

        // 设置GUI样式
        GUI.skin.button.fontSize = font_size_;
        GUI.skin.label.fontSize = font_size_;
        GUI.skin.textField.fontSize = font_size_;
        GUI.skin.textArea.fontSize = font_size_;
        GUI.skin.box.fontSize = font_size_;

        // 创建调试窗口
        Rect rect = new Rect(10, 10, Screen.width - 20, Screen.height - 20);
        GUI.Window(0, rect, DrawDebugWindow, "调试面板 - 按 " + toggle_key_ + " 关闭");
    }

    private void DrawDebugWindow(int window_id)
    {
        GUILayout.BeginVertical();

        // 标签页
        selected_tab_ = GUILayout.Toolbar(selected_tab_, tab_names_);

        GUILayout.Space(10);

        // 滚动视图
        scroll_position_ = GUILayout.BeginScrollView(scroll_position_, GUILayout.ExpandHeight(true));

        switch (selected_tab_)
        {
            case 0:
                DrawPerformanceTab();
                break;
            case 1:
                DrawLogTab();
                break;
            case 2:
                DrawSystemInfoTab();
                break;
            case 3:
                DrawVariablesTab();
                break;
            case 4:
                DrawQuickActionsTab();
                break;
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        GUI.DragWindow(new Rect(0, 0, Screen.width, 30));
    }

    #region 标签页绘制

    private void DrawPerformanceTab()
    {
        GUILayout.Label("=== 性能监控 ===", GetHeaderStyle());
        GUILayout.Space(10);

        // FPS
        Color original_color = GUI.color;
        if (fps_ < 30) GUI.color = Color.red;
        else if (fps_ < 50) GUI.color = Color.yellow;
        else GUI.color = Color.green;

        GUILayout.Label($"FPS: {fps_:F1}", GetBigTextStyle());
        GUI.color = original_color;

        // 其他性能数据
        GUILayout.Label($"帧时间: {delta_time_ * 1000:F2} ms");
        GUILayout.Label($"内存使用: {memory_usage_:F1} MB");
        GUILayout.Label($"Draw Calls: {draw_calls_}");

        GUILayout.Space(20);

        // FPS图表
        GUILayout.Label("FPS 历史记录", GetHeaderStyle());
        DrawFPSGraph();

        GUILayout.Space(20);

        // 性能建议
        GUILayout.Label("性能建议", GetHeaderStyle());
        if (fps_ < 30)
        {
            GUILayout.Label("⚠️ 游戏运行卡顿，建议：");
            GUILayout.Label("- 降低画质设置");
            GUILayout.Label("- 减少Draw Calls");
            GUILayout.Label("- 优化资源加载");
        }
        else if (fps_ < 50)
        {
            GUILayout.Label("⚡ 性能一般，可进一步优化");
        }
        else
        {
            GUILayout.Label("✅ 性能良好");
        }
    }

    private void DrawLogTab()
    {
        GUILayout.BeginHorizontal();

        // 过滤器
        GUILayout.Label("过滤器:", GUILayout.Width(60));
        show_info_logs_ = GUILayout.Toggle(show_info_logs_, "Info", GUILayout.Width(50));
        show_warning_ogs_ = GUILayout.Toggle(show_warning_ogs_, "Warning", GUILayout.Width(70));
        show_error_logs_ = GUILayout.Toggle(show_error_logs_, "Error", GUILayout.Width(50));

        GUILayout.Space(20);

        GUILayout.Label("搜索:", GUILayout.Width(40));
        log_filter_ = GUILayout.TextField(log_filter_, GUILayout.Width(150));

        if (GUILayout.Button("清除日志", GUILayout.Width(80)))
        {
            log_entries_.Clear();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 日志列表
        foreach (var log in log_entries_)
        {
            // 应用过滤器
            if (!ShouldShowLog(log)) continue;
            if (!string.IsNullOrEmpty(log_filter_) && !log.message_.ToLower().Contains(log_filter_.ToLower())) continue;

            // 设置颜色
            switch (log.type_)
            {
                case LogType.Log:
                    GUI.color = Color.white;
                    break;
                case LogType.Warning:
                    GUI.color = Color.yellow;
                    break;
                case LogType.Error:
                case LogType.Exception:
                    GUI.color = Color.red;
                    break;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{log.time_}]", GUILayout.Width(80));
            GUILayout.Label(log.message_, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("复制", GUILayout.Width(50)))
            {
                GUIUtility.systemCopyBuffer = log.message_;
            }

            GUILayout.EndHorizontal();
            GUI.color = Color.white;
        }
    }

    private void DrawSystemInfoTab()
    {
        GUILayout.Label("=== 系统信息 ===", GetHeaderStyle());
        GUILayout.Space(10);

        // 设备信息
        GUILayout.Label($"设备型号: {SystemInfo.deviceModel}");
        GUILayout.Label($"设备名称: {SystemInfo.deviceName}");
        GUILayout.Label($"操作系统: {SystemInfo.operatingSystem}");
        GUILayout.Label($"处理器: {SystemInfo.processorType} ({SystemInfo.processorCount} 核)");
        GUILayout.Label($"内存: {SystemInfo.systemMemorySize} MB");
        GUILayout.Label($"显卡: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB)");
        GUILayout.Label($"显卡版本: {SystemInfo.graphicsDeviceVersion}");
        GUILayout.Label($"屏幕分辨率: {Screen.width}x{Screen.height}");
        GUILayout.Label($"屏幕刷新率: {Screen.currentResolution.refreshRateRatio} Hz");
        GUILayout.Label($"电池电量: {SystemInfo.batteryLevel * 100:F0}%");

        GUILayout.Space(20);

        // Unity信息
        GUILayout.Label("=== Unity 信息 ===", GetHeaderStyle());
        GUILayout.Label($"Unity版本: {Application.unityVersion}");
        GUILayout.Label($"游戏版本: {Application.version}");
        GUILayout.Label($"平台: {Application.platform}");
        GUILayout.Label($"语言: {Application.systemLanguage}");
        GUILayout.Label($"数据路径: {Application.dataPath}");
        GUILayout.Label($"持久化路径: {Application.persistentDataPath}");
    }

    private void DrawVariablesTab()
    {
        GUILayout.Label("=== 游戏变量监控 ===", GetHeaderStyle());
        GUILayout.Space(10);

        string[] keys = game_variables_.Keys.ToArray();

        // 显示所有变量
        foreach (var key in keys)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{key}:", GUILayout.Width(150));

            object value = game_variables_[key];

            // 根据类型显示不同的控件
            if (value is int int_value)
            {
                int new_value = int_value;
                string strValue = GUILayout.TextField(int_value.ToString(), GUILayout.Width(100));
                if (int.TryParse(strValue, out new_value) && new_value != int_value)
                {
                    game_variables_[key] = new_value;
                }
            }
            else if (value is float float_value)
            {
                float new_value = float_value;
                string strValue = GUILayout.TextField(float_value.ToString("F2"), GUILayout.Width(100));
                if (float.TryParse(strValue, out new_value) && new_value != float_value)
                {
                    game_variables_[key] = new_value;
                }
            }
            else if (value is string string_value)
            {
                string new_value = GUILayout.TextField(string_value, GUILayout.Width(200));
                if (new_value != string_value)
                {
                    game_variables_[key] = new_value;
                }
            }
            else if (value is bool bool_value)
            {
                bool new_value = GUILayout.Toggle(bool_value, "");
                if (new_value != bool_value)
                {
                    game_variables_[key] = new_value;
                }
            }
            else
            {
                GUILayout.Label(value.ToString());
            }

            if (GUILayout.Button("删除", GUILayout.Width(50)))
            {
                game_variables_.Remove(key);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(20);

        // 添加新变量
        GUILayout.Label("添加新变量", GetHeaderStyle());
        GUILayout.BeginVertical();
        selected_var_type_ = GUILayout.Toolbar(selected_var_type_, var_types_);
        GUILayout.BeginHorizontal();
        new_var_key_ = GUILayout.TextField(new_var_key_, GUILayout.Width(150));
        new_var_value_ = GUILayout.TextField(new_var_value_, GUILayout.Width(150));

        if (GUILayout.Button("添加", GUILayout.Width(80)))
        {
            AddGameVariable(new_var_key_, ParseVariableValue(new_var_value_, selected_var_type_));
            new_var_key_ = "";
            new_var_value_ = "";
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void DrawQuickActionsTab()
    {
        GUILayout.Label("=== 快捷操作 ===", GetHeaderStyle());
        GUILayout.Space(10);



        // 时间控制
        GUILayout.Label("时间控制", GetHeaderStyle());
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("正常速度")) Time.timeScale = 1;
            if (GUILayout.Button("2倍速度")) Time.timeScale = 2;
            if (GUILayout.Button("5倍速度")) Time.timeScale = 5;
            if (GUILayout.Button("暂停")) Time.timeScale = 0;
            GUILayout.EndHorizontal();
            GUILayout.Label($"当前时间缩放: {Time.timeScale}x");
        }
        GUILayout.Space(20);



        // 场景操作
        GUILayout.Label("场景操作", GetHeaderStyle());
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("重新加载当前场景"))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
            if (GUILayout.Button("清除所有数据"))
            {
                PlayerPrefs.DeleteAll();
                game_variables_.Clear();
                Debug.Log("所有数据已清除");
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(20);




        // 调试选项
        GUILayout.Label("调试选项", GetHeaderStyle());
        {
            GUILayout.BeginHorizontal();

            // 显示FPS
            bool show_fps = GUILayout.Toggle(PlayerPrefs.GetInt("ShowFPS", 0) == 1, "显示FPS");
            if (show_fps != (PlayerPrefs.GetInt("ShowFPS", 0) == 1))
            {
                PlayerPrefs.SetInt("ShowFPS", show_fps ? 1 : 0);
            }

            // 无边框模式
            bool full_screen = GUILayout.Toggle(Screen.fullScreen, "全屏模式");
            if (full_screen != Screen.fullScreen)
            {
                Screen.fullScreen = full_screen;
            }

            // 垂直同步
            int v_sync_count = QualitySettings.vSyncCount;
            int new_v_sync = GUILayout.Toolbar(v_sync_count, new string[] { "关闭VSync", "开启VSync" });
            if (new_v_sync != v_sync_count)
            {
                QualitySettings.vSyncCount = new_v_sync;
            }

            GUILayout.EndHorizontal();
        }
        GUILayout.Space(20);




        // 自定义操作按钮 - 通过反射扫描所有标记了[DebugMethod]的工具类方法
        GUILayout.Label("自定义操作", GetHeaderStyle());
        DrawDynamicDebugMethods();
    }

    /// <summary>
    /// 通过反射扫描所有DebugToolBase子类中标记[DebugMethod]的方法并绘制按钮
    /// </summary>
    private void DrawDynamicDebugMethods()
    {
        EnsureDebugMethodsLoaded();

        string current_category = "";
        foreach (var entry in cached_debug_methods_)
        {
            // 分类标题
            if (entry.Category != current_category)
            {
                current_category = entry.Category;
                if (!string.IsNullOrEmpty(current_category))
                {
                    GUILayout.Space(5);
                    GUILayout.Label($"[{current_category}]", GetHeaderStyle());
                }
            }

            if (GUILayout.Button(entry.Label))
            {
                entry.Method.Invoke(entry.Instance, null);
            }
        }
    }

    /// <summary>
    /// 缓存：已扫描的调试方法列表
    /// </summary>
    private struct CachedDebugMethod
    {
        public string Label;
        public string Category;
        public MethodInfo Method;
        public object Instance;
    }

    private List<CachedDebugMethod> cached_debug_methods_ = null;

    private void EnsureDebugMethodsLoaded()
    {
        if (cached_debug_methods_ != null) return;
        cached_debug_methods_ = new List<CachedDebugMethod>();

        // 扫描当前程序集中所有 DebugToolBase 的子类
        var tool_base_type = typeof(DebugToolBase);
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !tool_base_type.IsAssignableFrom(type))
                continue;

            object instance = System.Activator.CreateInstance(type);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var attr = method.GetCustomAttribute<DebugMethodAttribute>();
                if (attr == null) continue;

                cached_debug_methods_.Add(new CachedDebugMethod
                {
                    Label = attr.ButtonLabel,
                    Category = attr.Category ?? type.Name,
                    Method = method,
                    Instance = instance
                });
            }
        }
    }

    #endregion

    #region 辅助方法

    private void HandleLog(string log, string stack_trace, LogType type)
    {
        log_entries_.Insert(0, new LogEntry
        {
            message_ = log,
            stack_trace_ = stack_trace,
            type_ = type,
            time_ = System.DateTime.Now.ToString("HH:mm:ss")
        });

        // 限制日志数量
        while (log_entries_.Count > 200)
        {
            log_entries_.RemoveAt(log_entries_.Count - 1);
        }
    }

    private bool ShouldShowLog(LogEntry log)
    {
        switch (log.type_)
        {
            case LogType.Log: return show_info_logs_;
            case LogType.Warning: return show_warning_ogs_;
            case LogType.Error:
            case LogType.Exception: return show_error_logs_;
            default: return true;
        }
    }

    private void AddGameVariable(string key, object value)
    {
        if (!string.IsNullOrEmpty(key) && !game_variables_.ContainsKey(key))
        {
            game_variables_[key] = value;
        }
    }

    /// <summary>
    /// 供外部工具类安全访问游戏变量
    /// </summary>
    public bool TryGetVariable(string key, out object value)
    {
        return game_variables_.TryGetValue(key, out value);
    }

    /// <summary>
    /// 供外部工具类安全设置游戏变量
    /// </summary>
    public void SetVariable(string key, object value)
    {
        game_variables_[key] = value;
    }

    private object ParseVariableValue(string value, int type)
    {
        switch (type)
        {
            case 0: // int
                if (int.TryParse(value, out int int_val)) return int_val;
                return 0;
            case 1: // float
                if (float.TryParse(value, out float float_val)) return float_val;
                return 0f;
            case 2: // string
                return value;
            case 3: // bool
                if (bool.TryParse(value, out bool bool_val)) return bool_val;
                return false;
            default:
                return value;
        }
    }

    private void DrawFPSGraph()
    {
        // 简化版FPS图表实现
        GUILayout.Box("FPS: " + fps_.ToString("F1"), GUILayout.Height(30), GUILayout.Width(200));
        Rect rect = GUILayoutUtility.GetLastRect();
        float percentage = Mathf.Clamp01(fps_ / 60f);
        Rect fill_rect = new Rect(rect.x, rect.y, rect.width * percentage, rect.height);
        GUI.Box(fill_rect, "", GUI.skin.button);
    }

    private GUIStyle GetHeaderStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = font_size_ + 2;
        return style;
    }

    private GUIStyle GetBigTextStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = font_size_ + 10;
        style.fontStyle = FontStyle.Bold;
        return style;
    }

    #endregion

    // 日志条目结构
    private class LogEntry
    {
        public string                               message_;
        public string                               stack_trace_;
        public LogType                              type_;
        public string                               time_;
    }
}