// using System.Collections.Generic;
// using UnityEngine;


// /// <summary>
// /// 玩家相关调试工具
// /// </summary>
// public class PlayerDebugTool : IDebugable
// {
//     private PlayerData              player_data_ = new PlayerData();
//     [DebugProperty]
//     public int                      slot_id_ = 1;
//     [DebugProperty]
//     public int                      slot_id_to_del_ = 1;


//     [DebugProperty]
//     public int                      player_id_ = 10001;
//     [DebugProperty]
//     public string                   player_name_ = "DebugPlayer";
//     [DebugProperty]
//     public int                      player_level_ = 1;
//     [DebugProperty]
//     public int                      player_score_ = 0;
//     [DebugProperty]
//     public float                    player_health_ = 100f;
//     [DebugProperty]
//     public bool                     player_is_alive_ = true;


//     [DebugMethod("创建新存档", "存档管理")]
//     public void CreateSaveData()
//     {
//         SaveManager.CreateSaveSlot("新存档");
//     }

//     [DebugMethod("创建新存档Async","存档管理")]
//     public async void CreateSaveDataAsync()
//     {
//         await SaveManager.CreateSaveSlotAsync("新存档Async");
//         Debug.Log("异步创建新存档完成");
//     }
    
//     [DebugMethod("切换存档槽位", "存档管理")]
//     public void SwitchSaveSlot()
//     {
//         if(DebugPanel.Instance.TryGetVariable(nameof(slot_id_), out object slot_id))
//         {
//             SaveManager.SwitchToSaveSlot((int)slot_id);
//             LoadPlayerData(); // 切换存档后加载玩家数据
//         }
//         else
//         {
//             Debug.LogWarning($"未找到变量 '{nameof(slot_id_)}'，请确保在面板中添加了该变量");
//         }
//     }

//     [DebugMethod("删除当前存档", "存档管理")]
//     public void DeleteCurrentSave()
//     {
//         SaveManager.DeleteSaveSlot(SaveManager.CurrentSlotId);
//     }

//     [DebugMethod("删除指定存档槽位", "存档管理")]
//     public void DeleteSpecifiedSaveSlot()
//     {
//         if(DebugPanel.Instance.TryGetVariable(nameof(slot_id_to_del_), out object slot_id))
//         {
//             SaveManager.DeleteSaveSlot((int)slot_id);
//         }
//         else
//         {
//             Debug.LogWarning($"未找到变量 '{nameof(slot_id_to_del_)}'，请确保在面板中添加了该变量");
//         }
//     }

//     [DebugMethod("删除所有存档", "存档管理")]
//     public void DeleteAllSaves()
//     {
//         SaveManager.DeleteAllSaveSlots();
//         Debug.Log("已删除所有存档");
//     }

//     [DebugMethod("保存玩家数据", "存档管理")]
//     public void SavePlayerData()
//     {
//         SaveManager.SaveData(player_data_);
//     }

//     [DebugMethod("加载玩家数据", "存档管理")]
//     public void LoadPlayerData()
//     {
//         player_data_ = SaveManager.ReadData<PlayerData>();
//         if (player_data_ != null)
//         {
//             Debug.Log($"玩家数据 - 等级: {player_data_.level_}, 分数: {player_data_.score_}, 名字: {player_data_.player_name_}, 健康: {player_data_.Health}, 是否存活: {player_data_.IsAlive}");
//         }
//         else
//         {
//             player_data_ = new PlayerData();
//             Debug.LogWarning("未找到玩家数据，已创建新的默认数据");
//         }
//         this.Publish(EventName.UPDATE_USERINFO, player_data_);
//     }

//     [DebugMethod("清除玩家数据", "存档管理")]
//     public void ClearPlayerData()
//     {
//         SaveManager.DeleteSaveData();
//         player_data_ = new PlayerData();
//         Debug.Log("玩家数据已清除");
//     }

//     [DebugMethod("打印所有存档数据", "存档管理")]
//     public void PrintAllSaveData()
//     {
//         List<SaveSlotInfo> all_data = SaveManager.GetAllSaveSlots();
//         if (all_data.Count > 0)
//         {
//             Debug.Log("所有存档数据:");
//             foreach (var kvp in all_data)
//             {
//                 Debug.Log($"存档ID: {kvp.slot_id_}, 存档名称: {kvp.slot_name_}, 存档时间: {kvp.create_time_}");
//             }
//         }
//         else
//         {
//             Debug.Log("没有找到任何存档数据");
//         }
//     }

//     [DebugMethod("打印当前存档ID", "存档管理")]
//     public void PrintCurrentSlotID()
//     {
//         int current_slot_id = SaveManager.CurrentSlotId;
//         if (current_slot_id >= 0)
//         {
//             Debug.Log($"当前存档槽位ID: {current_slot_id}");
//         }
//         else
//         {
//             Debug.Log($"当前没有激活的存档槽位ID: {current_slot_id}");
//         }
//     }

//     [DebugMethod("增加玩家血量", "玩家管理")]
//     public void IncreasePlayerHealth()
//     {
//         if(DebugPanel.Instance.TryGetVariable(nameof(player_health_), out object increase))
//         {
//             player_data_.Health += (float)increase;
//             SaveManager.SaveData(player_data_); // 血量变动后自动保存数据
//             this.Publish(EventName.UPDATE_USERINFO, player_data_);
//             Debug.Log($"玩家血量已增加，当前血量: {player_data_.Health}");
//         }
//         else
//         {
//             Debug.LogWarning($"未找到变量 '{nameof(player_health_)}'，请确保在面板中添加了该变量");
//         }
//     }

//     [DebugMethod("等级提升", "玩家管理")]
//     public void LevelUp()
//     {
//         if(DebugPanel.Instance.TryGetVariable(nameof(player_level_), out object data))
//         {
//             player_data_.level_ += (int)data;
//             SaveManager.SaveData(player_data_); // 等级变动后自动保存数据
//             this.Publish(EventName.UPDATE_USERINFO, player_data_);
//             Debug.Log($"玩家等级已提升，当前等级: {player_data_.level_}");
//         }
//         else
//         {
//             Debug.LogWarning($"未找到变量 '{nameof(player_level_)}'，请确保在面板中添加了该变量");
//         }
//     }

//     [DebugMethod("设置玩家名字", "玩家管理")]
//     public void SetPlayerName()
//     {
//         if(DebugPanel.Instance.TryGetVariable(nameof(player_name_), out object name))
//         {
//             player_data_.player_name_ = name.ToString();
//             SaveManager.SaveData(player_data_); // 名字变动后自动保存数据
//             this.Publish(EventName.UPDATE_USERINFO, player_data_);
//             Debug.Log($"玩家名字已设置为: {player_data_.player_name_}");
//         }
//         else
//         {
//             Debug.LogWarning($"未找到变量 '{nameof(player_name_)}'，请确保在面板中添加了该变量");
//         }
//     }

//     [DebugMethod("设置玩家ID", "玩家管理")]
//     public void SetPlayerID()
//     {
//         if(DebugPanel.Instance.TryGetVariable(nameof(player_id_), out object id))
//         {
//             player_data_.player_id_ = (int)id;
//             SaveManager.SaveData(player_data_); // ID变动后自动保存数据
//             this.Publish(EventName.UPDATE_USERINFO, player_data_);
//             Debug.Log($"玩家ID已设置为: {player_data_.player_id_}");
//         }
//         else
//         {
//             Debug.LogWarning($"未找到变量 '{nameof(player_id_)}'，请确保在面板中添加了该变量");
//         }
//     }

//     [DebugMethod("增加玩家分数", "玩家管理")]
//     public void IncreasePlayerScore()
//     {
//         if(DebugPanel.Instance.TryGetVariable(nameof(player_score_), out object increase))
//         {
//             player_data_.score_ += (int)increase;
//             SaveManager.SaveData(player_data_); // 分数变动后自动保存数据
//             this.Publish(EventName.UPDATE_USERINFO, player_data_);
//             Debug.Log($"玩家分数已增加，当前分数: {player_data_.score_}");
//         }
//         else
//         {
//             Debug.LogWarning($"未找到变量 '{nameof(player_score_)}'，请确保在面板中添加了该变量");
//         }
//     }
// }