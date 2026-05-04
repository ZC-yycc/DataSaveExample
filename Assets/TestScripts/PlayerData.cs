using Newtonsoft.Json;
using UnityEngine;

[Debugable]
public class PlayerData
{
    [DebugProperty]
    public string               player_name_ = "Player";
    
    [DebugProperty]
    public int                  player_id_ = 1;
    public int                  level_ = 1;
    public int                  score_ = 0;

    [JsonProperty, DebugProperty]
    private float               health_ = 100f;
    
    [JsonProperty]
    private bool                is_alive_ = true;

    public float Health
    {
        get => health_;
        set => health_ = Mathf.Clamp(value, 0, 100);
    }

    [DebugProperty]
    public bool IsAlive
    {
        get => is_alive_;
        set => is_alive_ = value;
    }

    [DebugMethod("Test PlayerData", "测试方法")]
    public void TestPlayerData()
    {
        if(DebugPanel.Instance.TryGetVariable(nameof(player_name_), out object player_name))
        {
            Debug.Log($"玩家名称: {player_name}");
        }
        else
        {
            Debug.LogWarning($"未找到变量 '{nameof(player_name_)}'，请确保在面板中添加了该变量");
        }

        if(DebugPanel.Instance.TryGetVariable(nameof(player_id_), out object player_id))
        {
            Debug.Log($"玩家ID: {player_id}");
        }
        else
        {
            Debug.LogWarning($"未找到变量 '{nameof(player_id_)}'，请确保在面板中添加了该变量");
        }

        if(DebugPanel.Instance.TryGetVariable(nameof(health_), out object health))
        {
            Debug.Log($"玩家血量: {health}");
        }
        else
        {
            Debug.LogWarning($"未找到变量 '{nameof(health_)}'，请确保在面板中添加了该变量");
        }

        if(DebugPanel.Instance.TryGetVariable(nameof(IsAlive), out object is_alive))
        {
            Debug.Log($"玩家是否存活: {is_alive}");
        }
        else
        {
            Debug.LogWarning($"未找到变量 '{nameof(IsAlive)}'，请确保在面板中添加了该变量");
        }
    }
}
