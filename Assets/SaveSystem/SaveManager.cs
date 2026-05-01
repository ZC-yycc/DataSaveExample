using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SaveJsonConverterBase<T> : JsonConverter<T>
{
    public override T? ReadJson(JsonReader reader, Type objectType, T? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return serializer.Deserialize<T>(reader);
    }

    public override void WriteJson(JsonWriter writer, T? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}

/// <summary>
/// 单个存档槽位的信息
/// </summary>
[Serializable]
public class SaveSlotInfo
{
    public int                                          slot_id_;
    public string                                       slot_name_;
    public string                                       create_time_;
    public string                                       last_save_time;

    public SaveSlotInfo(int slotId, string slotName)
    {
        slot_id_ = slotId;
        slot_name_ = slotName;
        create_time_ = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        last_save_time = create_time_;
    }
}

/// <summary>
/// 多存档元数据文件结构
/// </summary>
[Serializable]
public class SaveSlotsMeta
{
    public int                                          current_slot_id_ = 0;
    public List<SaveSlotInfo>                           slots_ = new List<SaveSlotInfo>();
}

public static class SaveManager
{
    private static JObject                              root_json_object_;
    private static JsonSerializerSettings               serializer_settings_;
    private static SaveSlotsMeta                        slots_meta_;
    private static int                                  current_slot_id_ = 0;

    private static string                               persistent_data_path_;
    private const string                                SLOTS_META_FILE = "save_slots_meta.json";   // 多存档元数据文件
    private const string                                SLOT_FILE_PREFIX = "save_slot_";             // 单个存档文件前缀
    private const string                                LEGACY_FILE_NAME = "game_save.json";         // 旧版单存档文件名
    private const string                                VERSION_KEY = "GameVersion";



    /// <summary>
    /// 获取当前激活的槽位ID
    /// </summary>
    public static int CurrentSlotId => current_slot_id_;



    static SaveManager()
    {
        persistent_data_path_ = Application.persistentDataPath;
        Application.quitting += OnApplicationQuit;

        InitSerializerSettings();
        LoadSlotsMeta();
    }

    private static void OnApplicationQuit()
    {
        // 退出前保存当前存档
        SaveCurrentSlotToFile();
        SaveSlotsMetaToFile();
    }



    #region 序列化设置与初始化
    private static void InitSerializerSettings()
    {
        List<JsonConverter> types = ConverterHelper.GetConverterInstances();

        serializer_settings_ = new()
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = types
        };
    }

    /// <summary>
    /// 加载多存档元数据，若不存在则尝试迁移旧版单存档
    /// </summary>
    private static void LoadSlotsMeta()
    {
        string meta_path = GetMetaFilePath();

        if (File.Exists(meta_path))
        {
            try
            {
                string json = File.ReadAllText(meta_path);
                slots_meta_ = JsonConvert.DeserializeObject<SaveSlotsMeta>(json);
                slots_meta_ ??= new SaveSlotsMeta();
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载存档元数据失败: {ex.Message}");
                slots_meta_ = new SaveSlotsMeta();
            }
        }
        else
        {
            slots_meta_ = new SaveSlotsMeta();

            // 尝试迁移旧版单存档文件
            TryMigrateLegacySave();
        }

        // 切换到当前槽位
        if (slots_meta_.slots_.Count > 0)
        {
            // 确保 current_slot_id 有效
            bool slot_exists = slots_meta_.slots_.Any(s => s.slot_id_ == slots_meta_.current_slot_id_);
            if (!slot_exists)
            {
                slots_meta_.current_slot_id_ = slots_meta_.slots_[0].slot_id_;
            }
            SwitchToSlotInternal(slots_meta_.current_slot_id_);
        }
        else
        {
            // 没有任何存档槽位，创建默认槽位
            CreateSaveSlot("默认存档");
        }
    }

    /// <summary>
    /// 尝试将旧版单存档文件迁移为新的多存档槽位
    /// </summary>
    private static void TryMigrateLegacySave()
    {
        string legacy_path = Path.Combine(persistent_data_path_, LEGACY_FILE_NAME);
        if (File.Exists(legacy_path))
        {
            try
            {
                // 创建新槽位
                SaveSlotInfo slot = new SaveSlotInfo(0, "迁移的存档");
                slots_meta_.slots_.Add(slot);
                slots_meta_.current_slot_id_ = 0;

                // 将旧存档内容复制到新槽位文件
                string new_path = GetSlotFilePath(0);
                File.Copy(legacy_path, new_path, overwrite: true);

                // 删除旧文件
                File.Delete(legacy_path);

                // 保存元数据
                SaveSlotsMetaToFile();

                Debug.Log("已将旧版存档迁移到多存档系统");
            }
            catch (Exception ex)
            {
                Debug.LogError($"迁移旧版存档失败: {ex.Message}");
            }
        }
    }
    #endregion



    #region 存档槽位管理
    /// <summary>
    /// 创建一个新的存档槽位
    /// </summary>
    /// <param name="slot_name">槽位名称</param>
    /// <returns>新创建的槽位信息</returns>
    public static SaveSlotInfo CreateSaveSlot(string slot_name)
    {
        // 自动生成槽位ID（使用最大ID + 1）
        int new_id = slots_meta_.slots_.Count > 0
            ? slots_meta_.slots_.Max(s => s.slot_id_) + 1
            : 0;

        SaveSlotInfo new_slot = new SaveSlotInfo(new_id, slot_name);
        slots_meta_.slots_.Add(new_slot);

        // 如果这是第一个槽位，自动激活它
        if (slots_meta_.slots_.Count == 1)
        {
            slots_meta_.current_slot_id_ = new_id;
            root_json_object_ = new JObject();
            SaveCurrentSlotToFile();
        }

        SaveSlotsMetaToFile();
        Debug.Log($"已创建存档槽位: [{new_id}] {slot_name}");
        return new_slot;
    }

    /// <summary>
    /// 删除指定存档槽位
    /// </summary>
    /// <param name="slot_id">要删除的槽位ID</param>
    /// <returns>是否删除成功</returns>
    public static bool DeleteSaveSlot(int slot_id)
    {
        SaveSlotInfo slot = slots_meta_.slots_.Find(s => s.slot_id_ == slot_id);
        if (slot == null)
        {
            Debug.LogWarning($"未找到存档槽位 ID: {slot_id}");
            return false;
        }

        // 如果删除的是当前激活的槽位，需要先保存并切换
        if (slot_id == current_slot_id_)
        {
            SaveCurrentSlotToFile();
            root_json_object_ = new JObject();
        }

        // 删除槽位文件
        string slot_path = GetSlotFilePath(slot_id);
        if (File.Exists(slot_path))
        {
            try
            {
                File.Delete(slot_path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除存档文件失败: {ex.Message}");
            }
        }

        // 从元数据中移除
        slots_meta_.slots_.Remove(slot);

        // 如果删除的是当前槽位，切换到第一个可用槽位
        if (slot_id == current_slot_id_)
        {
            if (slots_meta_.slots_.Count > 0)
            {
                slots_meta_.current_slot_id_ = slots_meta_.slots_[0].slot_id_;
                SwitchToSlotInternal(slots_meta_.current_slot_id_);
            }
            else
            {
                slots_meta_.current_slot_id_ = 0;
                current_slot_id_ = -1; // 无槽位
            }
        }

        SaveSlotsMetaToFile();
        Debug.Log($"已删除存档槽位: [{slot_id}] {slot.slot_name_}");
        return true;
    }

    /// <summary>
    /// 切换到指定存档槽位
    /// </summary>
    /// <param name="slot_id">目标槽位ID</param>
    /// <returns>是否切换成功</returns>
    public static bool SwitchToSaveSlot(int slot_id)
    {
        return SwitchToSlotById(slot_id, save_current: true);
    }

    /// <summary>
    /// 切换到指定存档槽位（可选择是否保存当前存档）
    /// </summary>
    /// <param name="slot_id">目标槽位ID</param>
    /// <param name="save_current">是否在切换前保存当前存档</param>
    /// <returns>是否切换成功</returns>
    public static bool SwitchToSaveSlot(int slot_id, bool save_current)
    {
        return SwitchToSlotById(slot_id, save_current);
    }

    /// <summary>
    /// 重命名当前存档槽位
    /// </summary>
    /// <param name="new_name">新名称</param>
    public static void RenameCurrentSlot(string new_name)
    {
        SaveSlotInfo slot = slots_meta_.slots_.Find(s => s.slot_id_ == current_slot_id_);
        if (slot != null)
        {
            slot.slot_name_ = new_name;
            SaveSlotsMetaToFile();
            Debug.Log($"存档槽位 [{current_slot_id_}] 已重命名为: {new_name}");
        }
        else
        {
            Debug.LogWarning("当前没有激活的存档槽位");
        }
    }

    /// <summary>
    /// 删除所有存档槽位
    /// </summary>
    public static void DeleteAllSaveSlots()
    {
        // 删除所有槽位文件
        foreach (var slot in slots_meta_.slots_)
        {
            string path = GetSlotFilePath(slot.slot_id_);
            if (File.Exists(path))
            {
                try { File.Delete(path); }
                catch (Exception ex) { Debug.LogError($"删除存档文件失败: {ex.Message}"); }
            }
        }

        // 重置元数据
        slots_meta_ = new SaveSlotsMeta();
        root_json_object_ = new JObject();
        current_slot_id_ = -1;

        SaveSlotsMetaToFile();

        // 创建一个默认槽位
        CreateSaveSlot("默认存档");
        Debug.Log("已删除所有存档并创建新的默认存档");
    }

    /// <summary>
    /// 获取当前激活的槽位信息
    /// </summary>
    public static SaveSlotInfo GetCurrentSlotInfo()
    {
        return slots_meta_.slots_.Find(s => s.slot_id_ == current_slot_id_);
    }

    /// <summary>
    /// 获取所有存档槽位信息
    /// </summary>
    public static List<SaveSlotInfo> GetAllSaveSlots()
    {
        return new List<SaveSlotInfo>(slots_meta_.slots_);
    }

    /// <summary>
    /// 获取存档槽位数量
    /// </summary>
    public static int GetSaveSlotCount()
    {
        return slots_meta_.slots_.Count;
    }

    /// <summary>
    /// 根据ID获取槽位信息
    /// </summary>
    public static SaveSlotInfo GetSlotInfo(int slot_id)
    {
        return slots_meta_.slots_.Find(s => s.slot_id_ == slot_id);
    }
    #endregion



    #region 保存数据
    /// <summary>
    /// 保存数据到当前存档（key 默认为类型名）
    /// </summary>
    public static void SaveData<T>(T obj, string key = null) where T : class
    {
        try
        {
            key ??= typeof(T).Name;
            string obj_json = JsonConvert.SerializeObject(obj, Formatting.Indented, serializer_settings_);
            JToken obj_node = JToken.Parse(obj_json);
            root_json_object_[key] = obj_node;
            SaveCurrentSlotToFile();
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSON 序列化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存数据到当前存档（指定 key 和 object）
    /// </summary>
    public static void SaveData(object obj, string key)
    {
        try
        {
            string obj_json = JsonConvert.SerializeObject(obj, Formatting.Indented, serializer_settings_);
            JToken obj_node = JToken.Parse(obj_json);
            root_json_object_[key] = obj_node;
            SaveCurrentSlotToFile();
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSON 序列化失败: {ex.Message}");
        }
    }
    #endregion



    #region 读取数据
    /// <summary>
    /// 从当前存档读取数据（key 默认为类型名）
    /// </summary>
    public static T ReadData<T>(string key = null) where T : class
    {
        key ??= typeof(T).Name;
        try
        {
            if (root_json_object_.TryGetValue(key, out JToken token))
            {
                return JsonConvert.DeserializeObject<T>(token.ToString(), serializer_settings_);
            }
            Debug.LogWarning($"未找到 {key} 类型的存档数据");
            return default;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取{nameof(T)}数据失败：{e.Message}");
            return default;
        }
    }

    /// <summary>
    /// 从当前存档读取数据（指定 key 和 Type）
    /// </summary>
    public static object ReadData(string key, Type type)
    {
        try
        {
            if (root_json_object_.TryGetValue(key, out JToken token))
            {
                return JsonConvert.DeserializeObject(token.ToString(), type, serializer_settings_);
            }
            Debug.LogWarning($"未找到 {key} 类型的存档数据");
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取{key}数据失败：{e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 尝试从当前存档读取数据（key 默认为类型名）
    /// </summary>
    public static bool TryReadData<T>(out T data, string key = null) where T : class
    {
        key ??= typeof(T).Name;
        data = null;
        try
        {
            if (root_json_object_.TryGetValue(key, out JToken token))
            {
                data = JsonConvert.DeserializeObject<T>(token.ToString(), serializer_settings_);
                return true;
            }
            Debug.LogWarning($"未找到 {key} 类型的存档数据");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取{nameof(T)}数据失败：{e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 尝试从当前存档读取数据（指定 key 和 Type）
    /// </summary>
    public static bool TryReadData(string key, Type type, out object data)
    {
        data = null;
        try
        {
            if (root_json_object_.TryGetValue(key, out JToken token))
            {
                data = JsonConvert.DeserializeObject(token.ToString(), type, serializer_settings_);
                return true;
            }
            Debug.LogWarning($"未找到 {key} 类型的存档数据");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"读取{key}数据失败：{e.Message}");
            return false;
        }
    }
    #endregion



    #region 检查数据
    /// <summary>
    /// 检查当前存档中是否存在指定 key 的数据
    /// </summary>
    public static bool HasData<T>(string key = null) where T : class
    {
        key ??= typeof(T).Name;
        return root_json_object_.ContainsKey(key);
    }

    /// <summary>
    /// 检查当前存档中是否存在指定 key 的数据
    /// </summary>
    public static bool HasData(string key)
    {
        return root_json_object_.ContainsKey(key);
    }
    #endregion



    #region 删除数据
    /// <summary>
    /// 删除当前存档中指定 key 的数据
    /// </summary>
    public static void DeleteData<T>(string key = null) where T : class
    {
        key ??= typeof(T).Name;
        if (root_json_object_.Remove(key))
        {
            SaveCurrentSlotToFile();
            Debug.Log($"已删除 {key} 类型的存档数据");
        }
        else
        {
            Debug.LogWarning($"未找到 {key} 类型的存档数据，无法删除");
        }
    }

    /// <summary>
    /// 删除当前存档中指定 key 的数据
    /// </summary>
    public static void DeleteData(string key)
    {
        if (root_json_object_.Remove(key))
        {
            SaveCurrentSlotToFile();
            Debug.Log($"已删除 {key} 类型的存档数据");
        }
        else
        {
            Debug.LogWarning($"未找到 {key} 类型的存档数据，无法删除");
        }
    }

    /// <summary>
    /// 重置当前存档槽位（清空所有数据）
    /// </summary>
    public static void DeleteSaveData()
    {
        root_json_object_ = new JObject();
        SaveCurrentSlotToFile();
        Debug.Log("已清空当前存档数据");

        // 更新槽位的最后保存时间
        UpdateCurrentSlotLastSaveTime();
    }
    #endregion



    #region 辅助方法 — 内部槽位切换逻辑
    /// <summary>
    /// 内部槽位切换方法
    /// </summary>
    private static bool SwitchToSlotById(int slot_id, bool save_current)
    {
        SaveSlotInfo target_slot = slots_meta_.slots_.Find(s => s.slot_id_ == slot_id);
        if (target_slot == null)
        {
            Debug.LogWarning($"未找到存档槽位 ID: {slot_id}");
            return false;
        }

        if (slot_id == current_slot_id_)
        {
            Debug.Log($"已经在存档槽位 [{slot_id}] 中，无需切换");
            return true;
        }

        // 切换前保存当前槽位
        if (save_current && current_slot_id_ >= 0)
        {
            SaveCurrentSlotToFile();
        }

        // 切换到目标槽位
        SwitchToSlotInternal(slot_id);
        Debug.Log($"已切换到存档槽位: [{slot_id}] {target_slot.slot_name_}");
        return true;
    }

    /// <summary>
    /// 加载指定槽位的数据到内存
    /// </summary>
    private static void SwitchToSlotInternal(int slot_id)
    {
        current_slot_id_ = slot_id;
        slots_meta_.current_slot_id_ = slot_id;
        SaveSlotsMetaToFile();

        string slot_path = GetSlotFilePath(slot_id);

        if (File.Exists(slot_path))
        {
            try
            {
                string json = File.ReadAllText(slot_path);
                if (!string.IsNullOrEmpty(json))
                {
                    root_json_object_ = JObject.Parse(json);
                    string saved_version = root_json_object_.Value<string>(VERSION_KEY);
                    if (saved_version != Application.version)
                    {
                        Debug.Log("开发模式：由于版本变更，重置当前存档");
                        root_json_object_ = new JObject();
                    }
                }
                else
                {
                    root_json_object_ = new JObject();
                }
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载存档槽位 [{slot_id}] 失败: {ex.Message}");
            }
        }

        // 文件不存在或加载失败，初始化空存档
        root_json_object_ = new JObject();
        SaveCurrentSlotToFile();
    }

    /// <summary>
    /// 更新当前槽位的最后保存时间
    /// </summary>
    private static void UpdateCurrentSlotLastSaveTime()
    {
        SaveSlotInfo current_slot = slots_meta_.slots_.Find(s => s.slot_id_ == current_slot_id_);
        if (current_slot != null)
        {
            current_slot.last_save_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveSlotsMetaToFile();
        }
    }

    /// <summary>
    /// 将当前 root_json_object_ 写入当前槽位文件
    /// </summary>
    private static void SaveCurrentSlotToFile()
    {
        if (current_slot_id_ < 0) return;

        root_json_object_[VERSION_KEY] = Application.version;
        string json = root_json_object_.ToString(Formatting.Indented);
        string path = GetSlotFilePath(current_slot_id_);
        File.WriteAllText(path, json);

        // 更新最后保存时间
        SaveSlotInfo current_slot = slots_meta_.slots_.Find(s => s.slot_id_ == current_slot_id_);
        if (current_slot != null)
        {
            current_slot.last_save_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // 注意: 不在此处调用 SaveSlotsMetaToFile()，因为 SaveCurrentSlotToFile
        // 在 SaveData 中被频繁调用。改为在槽位切换和退出时统一保存元数据。
        // 但为了确保 LastSaveTime 不丢失，这里也保存元数据（开销很小）
        SaveSlotsMetaToFile();
    }

    /// <summary>
    /// 将槽位元数据写入文件
    /// </summary>
    private static void SaveSlotsMetaToFile()
    {
        try
        {
            string json = JsonConvert.SerializeObject(slots_meta_, Formatting.Indented);
            string path = GetMetaFilePath();
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存存档元数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取元数据文件路径
    /// </summary>
    private static string GetMetaFilePath()
    {
        return Path.Combine(persistent_data_path_, SLOTS_META_FILE);
    }

    /// <summary>
    /// 获取指定槽位的存档文件路径
    /// </summary>
    private static string GetSlotFilePath(int slot_id)
    {
        return Path.Combine(persistent_data_path_, $"{SLOT_FILE_PREFIX}{slot_id}.json");
    }
    #endregion
}