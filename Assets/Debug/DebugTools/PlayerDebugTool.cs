using System.Collections.Generic;
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
            this.Publish(EventName.UPDATE_USERINFO, player_data_);
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
        player_data_ = new PlayerData();
        this.Publish(EventName.UPDATE_USERINFO, player_data_);
        Debug.Log("玩家数据已清除");
    }

    [DebugMethod("打印所有存档数据")]
    public void PrintAllSaveData()
    {
        List<SaveSlotInfo> all_data = SaveManager.GetAllSaveSlots();
        if (all_data.Count > 0)
        {
            Debug.Log("所有存档数据:");
            foreach (var kvp in all_data)
            {
                Debug.Log($"存档ID: {kvp.slot_id_}, 存档名称: {kvp.slot_name_}, 存档时间: {kvp.create_time_}");
            }
        }
        else
        {
            Debug.Log("没有找到任何存档数据");
        }
    }

    [DebugMethod("删除当前存档")]
    public void DeleteCurrentSave()
    {
        SaveManager.DeleteSaveSlot(SaveManager.CurrentSlotId);
    }

    [DebugMethod("切换存档槽位")]
    public void SwitchSaveSlot()
    {
        if(Panel.TryGetVariable("SlotID", out object slot_id))
        {
            SaveManager.SwitchToSaveSlot((int)slot_id);
            Debug.Log($"已切换到存档槽位: {(int)slot_id}");
        }
        else
        {
            Debug.LogWarning("未找到变量 'SlotID'，请确保在面板中添加了该变量");
        }
    }

    [DebugMethod("删除指定存档槽位")]
    public void DeleteSpecifiedSaveSlot()
    {
        if(Panel.TryGetVariable("SlotIDToDel", out object slot_id))
        {
            SaveManager.DeleteSaveSlot((int)slot_id);
            Debug.Log($"已删除存档槽位: {(int)slot_id}");
        }
        else
        {
            Debug.LogWarning("未找到变量 'SlotIDToDel'，请确保在面板中添加了该变量");
        }
    }

    [DebugMethod("增加玩家血量")]
    public void IncreasePlayerHealth()
    {
        if(Panel.TryGetVariable("HealthIncrease", out object increase))
        {
            player_data_.Health += (float)increase;
            this.Publish(EventName.UPDATE_USERINFO, player_data_);
            Debug.Log($"玩家血量已增加，当前血量: {player_data_.Health}");
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
            this.Publish(EventName.UPDATE_USERINFO, player_data_);
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
            this.Publish(EventName.UPDATE_USERINFO, player_data_);
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
            this.Publish(EventName.UPDATE_USERINFO, player_data_);
            Debug.Log($"玩家ID已设置为: {player_data_.player_id_}");
        }
        else
        {
            Debug.LogWarning("未找到变量 'PlayerID'，请确保在面板中添加了该变量");
        }
    }

    [DebugMethod("增加玩家分数")]
    public void IncreasePlayerScore()
    {
        if(Panel.TryGetVariable("ScoreIncrease", out object increase))
        {
            player_data_.score_ += (int)increase;
            this.Publish(EventName.UPDATE_USERINFO, player_data_);
            Debug.Log($"玩家分数已增加，当前分数: {player_data_.score_}");
        }
        else
        {
            Debug.LogWarning("未找到变量 'ScoreIncrease'，请确保在面板中添加了该变量");
        }
    }
}