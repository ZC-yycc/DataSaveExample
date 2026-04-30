using UnityEngine;

/// <summary>
/// 玩家相关调试工具
/// </summary>
public class PlayerDebugTool : DebugToolBase
{
    private PlayerData player_data_ = new PlayerData();

    [DebugMethod("保存玩家数据")]
    public void SavePlayerData()
    {
        SaveManager.SaveData(player_data_);
    }

    [DebugMethod("加载玩家数据")]
    public void LoadPlayerData()
    {
        player_data_ = SaveManager.ReadData<PlayerData>();
        if (player_data_ != null)
        {
            Debug.Log($"玩家数据 - 等级: {player_data_.level_}, 分数: {player_data_.score_}, 名字: {player_data_.player_name_}, 健康: {player_data_.Health}, 是否存活: {player_data_.IsAlive}");
        }
        else
        {
            Debug.Log("没有找到玩家数据");
        }
    }

    [DebugMethod("清除玩家数据")]
    public void ClearPlayerData()
    {
        SaveManager.DeleteSaveData();
        Debug.Log("玩家数据已清除");
    }

    [DebugMethod("增加玩家血量")]
    public void IncreasePlayerHealth()
    {
        if(Panel.TryGetVariable("HealthIncrease", out object increase))
        {
            player_data_.Health += (float)increase;
        }
        else
        {
            Debug.LogWarning("未找到变量 'HealthIncrease'，请确保在面板中添加了该变量");
        }
    }

    [DebugMethod("等级提升")]
    public void LevelUp()
    {
        if(Panel.TryGetVariable("Level", out object data))
        {
            player_data_.level_ += (int)data;
            Debug.Log($"玩家等级已提升，当前等级: {player_data_.level_}");
        }
        else
        {
            Debug.LogWarning("未找到变量 'Level'，请确保在面板中添加了该变量");
        }
    }

    [DebugMethod("设置玩家名字")]
    public void SetPlayerName()
    {
        if(Panel.TryGetVariable("PlayerName", out object name))
        {
            player_data_.player_name_ = name.ToString();
            Debug.Log($"玩家名字已设置为: {player_data_.player_name_}");
        }
        else
        {
            Debug.LogWarning("未找到变量 'PlayerName'，请确保在面板中添加了该变量");
        }
    }

    [DebugMethod("设置玩家ID")]
    public void SetPlayerID()
    {
        if(Panel.TryGetVariable("PlayerID", out object id))
        {
            player_data_.player_id_ = (int)id;
            Debug.Log($"玩家ID已设置为: {player_data_.player_id_}");
        }
        else
        {
            Debug.LogWarning("未找到变量 'PlayerID'，请确保在面板中添加了该变量");
        }
    }
}