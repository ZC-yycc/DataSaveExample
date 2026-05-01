using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    public SaveSlotInfo(int slot_id, string slot_name)
    {
        slot_id_ = slot_id;
        slot_name_ = slot_name;
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
    public int                                          current_slot_id_ = -1; // -1 表示无激活槽位
    public List<SaveSlotInfo>                           slots_ = new List<SaveSlotInfo>();
}

public static class SaveManager
{
    private static JObject                              root_json_object_ = new JObject(); // 当前存档的内存数据对象
    private static JsonSerializerSettings               serializer_settings_;
    private static SaveSlotsMeta                        slots_meta_;
    private static int                                  current_slot_id_ = -1;

    private static string                               persistent_data_path_;
    private const string                                SLOTS_META_FILE = "save_slots_meta.json";   // 多存档元数据文件
    private const string                                SLOT_FILE_PREFIX = "save_slot_";             // 单个存档文件前缀
    private const string                                LEGACY_FILE_NAME = "game_save.json";         // 旧版单存档文件名
    private const string                                VERSION_KEY = "GameVersion";

    /// <summary>
    /// 是否有异步操作正在进行中
    /// </summary>
    public static bool IsBusy { get; private set; } = false;



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
        // 退出前保存当前存档（同步，确保写入完成）
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

        // 如果存在之前激活的槽位，自动加载
        if (slots_meta_.slots_.Count > 0 && slots_meta_.current_slot_id_ >= 0)
        {
            bool slot_exists = slots_meta_.slots_.Any(s => s.slot_id_ == slots_meta_.current_slot_id_);
            if (slot_exists)
            {
                SwitchToSlotInternal(slots_meta_.current_slot_id_);
            }
        }
    }

    /// <summary>
    /// 异步加载多存档元数据
    /// </summary>
    private static async Task LoadSlotsMetaAsync()
    {
        string meta_path = GetMetaFilePath();

        if (File.Exists(meta_path))
        {
            try
            {
                string json = await Task.Run(() => File.ReadAllText(meta_path));
                slots_meta_ = await Task.Run(() => JsonConvert.DeserializeObject<SaveSlotsMeta>(json));
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

        // 如果存在之前激活的槽位，自动加载
        if (slots_meta_.slots_.Count > 0 && slots_meta_.current_slot_id_ >= 0)
        {
            bool slot_exists = slots_meta_.slots_.Any(s => s.slot_id_ == slots_meta_.current_slot_id_);
            if (slot_exists)
            {
                await SwitchToSlotInternalAsync(slots_meta_.current_slot_id_);
            }
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



    #region 存档槽位管理（同步）
    /// <summary>
    /// 创建一个新的存档槽位，如果这是第一个槽位，则自动激活它
    /// </summary>
    /// <param name="slot_name">槽位名称</param>
    /// <returns>新创建的槽位信息</returns>
    public static SaveSlotInfo CreateSaveSlot(string slot_name)
    {
        // 自动生成槽位ID（使用最大ID + 1）
        int new_id = slots_meta_.slots_.Count > 0 ? 
            slots_meta_.slots_.Max(s => s.slot_id_) + 1 : 0;

        SaveSlotInfo new_slot = new SaveSlotInfo(new_id, slot_name);
        slots_meta_.slots_.Add(new_slot);

        // 如果这是第一个槽位，自动激活它
        if (slots_meta_.slots_.Count == 1)
        {
            slots_meta_.current_slot_id_ = new_id;
            current_slot_id_ = new_id;
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
        root_json_object_ = new JObject(); // 清空内存数据
        slots_meta_.slots_.Remove(slot);
        slots_meta_.current_slot_id_ = -1; // 无激活槽位
        current_slot_id_ = -1; // 无槽位

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



    #region 存档槽位管理（异步）
    /// <summary>
    /// [异步] 创建一个新的存档槽位，如果这是第一个槽位，则自动激活它
    /// </summary>
    public static async Task<SaveSlotInfo> CreateSaveSlotAsync(string slot_name)
    {
        int new_id = slots_meta_.slots_.Count > 0 ?
            slots_meta_.slots_.Max(s => s.slot_id_) + 1 : 0;

        SaveSlotInfo new_slot = new SaveSlotInfo(new_id, slot_name);
        slots_meta_.slots_.Add(new_slot);

        if (slots_meta_.slots_.Count == 1)
        {
            slots_meta_.current_slot_id_ = new_id;
            current_slot_id_ = new_id;
            await SaveCurrentSlotToFileAsync();
        }

        await SaveSlotsMetaToFileAsync();
        Debug.Log($"已创建存档槽位: [{new_id}] {slot_name}");
        return new_slot;
    }

    /// <summary>
    /// [异步] 删除指定存档槽位
    /// </summary>
    public static async Task<bool> DeleteSaveSlotAsync(int slot_id)
    {
        SaveSlotInfo slot = slots_meta_.slots_.Find(s => s.slot_id_ == slot_id);
        if (slot == null)
        {
            Debug.LogWarning($"未找到存档槽位 ID: {slot_id}");
            return false;
        }

        string slot_path = GetSlotFilePath(slot_id);
        if (File.Exists(slot_path))
        {
            try
            {
                await Task.Run(() => File.Delete(slot_path));
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除存档文件失败: {ex.Message}");
            }
        }

        root_json_object_ = new JObject();
        slots_meta_.slots_.Remove(slot);
        slots_meta_.current_slot_id_ = -1;
        current_slot_id_ = -1;

        await SaveSlotsMetaToFileAsync();
        Debug.Log($"已删除存档槽位: [{slot_id}] {slot.slot_name_}");
        return true;
    }

    /// <summary>
    /// [异步] 切换到指定存档槽位
    /// </summary>
    /// <param name="slot_id">目标槽位ID</param>
    /// <param name="save_current">是否在切换前保存当前存档</param>
    /// <returns>是否切换成功</returns>
    public static async Task<bool> SwitchToSaveSlotAsync(int slot_id, bool save_current = true)
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
            await SaveCurrentSlotToFileAsync();
        }

        // 异步加载目标槽位
        await SwitchToSlotInternalAsync(slot_id);
        Debug.Log($"已切换到存档槽位: [{slot_id}] {target_slot.slot_name_}");
        return true;
    }

    /// <summary>
    /// [异步] 删除所有存档槽位
    /// </summary>
    public static async Task DeleteAllSaveSlotsAsync()
    {
        foreach (var slot in slots_meta_.slots_)
        {
            string path = GetSlotFilePath(slot.slot_id_);
            if (File.Exists(path))
            {
                try { await Task.Run(() => File.Delete(path)); }
                catch (Exception ex) { Debug.LogError($"删除存档文件失败: {ex.Message}"); }
            }
        }

        slots_meta_ = new SaveSlotsMeta();
        root_json_object_ = new JObject();
        current_slot_id_ = -1;

        await SaveSlotsMetaToFileAsync();
        Debug.Log("已删除所有存档");
    }
    #endregion



    #region 保存数据（同步）
    /// <summary>
    /// 保存数据到当前存档（key 默认为类型名）
    /// </summary>
    public static void SaveData<T>(T obj, string key = null) where T : class
    {
        try
        {
            EnsureActiveSlot();
            key ??= typeof(T).Name;
            string obj_json = JsonConvert.SerializeObject(obj, Formatting.Indented, serializer_settings_);
            JToken obj_node = JToken.Parse(obj_json);
            root_json_object_[key] = obj_node;
            SaveCurrentSlotToFile();
            Debug.Log($"已保存 {key} 类型的存档数据");
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
            EnsureActiveSlot();
            string obj_json = JsonConvert.SerializeObject(obj, Formatting.Indented, serializer_settings_);
            JToken obj_node = JToken.Parse(obj_json);
            root_json_object_[key] = obj_node;
            SaveCurrentSlotToFile();
            Debug.Log($"已保存 {key} 类型的存档数据");
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSON 序列化失败: {ex.Message}");
        }
    }
    #endregion



    #region 保存数据（异步）
    /// <summary>
    /// [异步] 保存数据到当前存档（key 默认为类型名）
    /// </summary>
    public static async Task SaveDataAsync<T>(T obj, string key = null) where T : class
    {
        try
        {
            await EnsureActiveSlotAsync();
            key ??= typeof(T).Name;

            string obj_json = await Task.Run(() =>
                JsonConvert.SerializeObject(obj, Formatting.Indented, serializer_settings_));

            JToken obj_node = await Task.Run(() => JToken.Parse(obj_json));
            root_json_object_[key] = obj_node;

            await SaveCurrentSlotToFileAsync();
            Debug.Log($"已保存 {key} 类型的存档数据");
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSON 序列化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// [异步] 保存数据到当前存档（指定 key 和 object）
    /// </summary>
    public static async Task SaveDataAsync(object obj, string key)
    {
        try
        {
            await EnsureActiveSlotAsync();
            string obj_json = await Task.Run(() =>
                JsonConvert.SerializeObject(obj, Formatting.Indented, serializer_settings_));

            JToken obj_node = await Task.Run(() => JToken.Parse(obj_json));
            root_json_object_[key] = obj_node;

            await SaveCurrentSlotToFileAsync();
            Debug.Log($"已保存 {key} 类型的存档数据");
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSON 序列化失败: {ex.Message}");
        }
    }
    #endregion



    #region 读取数据（同步）
    /// <summary>
    /// 从当前存档读取数据（key 默认为类型名）
    /// </summary>
    public static T ReadData<T>(string key = null) where T : class
    {
        try
        {
            key ??= typeof(T).Name;
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
        data = null;
        try
        {
            key ??= typeof(T).Name;
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



    #region 读取数据（异步）
    /// <summary>
    /// [异步] 从当前存档读取数据（key 默认为类型名）
    /// </summary>
    public static async Task<T> ReadDataAsync<T>(string key = null) where T : class
    {
        try
        {
            key ??= typeof(T).Name;
            if (root_json_object_.TryGetValue(key, out JToken token))
            {
                string token_str = token.ToString();
                return await Task.Run(() =>
                    JsonConvert.DeserializeObject<T>(token_str, serializer_settings_));
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
    /// [异步] 从当前存档读取数据（指定 key 和 Type）
    /// </summary>
    public static async Task<object> ReadDataAsync(string key, Type type)
    {
        try
        {
            if (root_json_object_.TryGetValue(key, out JToken token))
            {
                string token_str = token.ToString();
                return await Task.Run(() =>
                    JsonConvert.DeserializeObject(token_str, type, serializer_settings_));
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
        Debug.Log("已清空当前存档数据");
        SaveCurrentSlotToFile();
        UpdateCurrentSlotLastSaveTime();
    }

    /// <summary>
    /// [异步] 重置当前存档槽位（清空所有数据）
    /// </summary>
    public static async Task DeleteSaveDataAsync()
    {
        root_json_object_ = new JObject();
        Debug.Log("已清空当前存档数据");
        await SaveCurrentSlotToFileAsync();
        UpdateCurrentSlotLastSaveTime();
    }
    #endregion



    #region 初始化辅助
    /// <summary>
    /// [异步] 初始化系统（异步加载元数据和上次激活的槽位）
    /// </summary>
    public static async Task InitializeAsync()
    {
        await LoadSlotsMetaAsync();
    }

    /// <summary>
    /// 确保当前有激活的存档槽位（同步），没有则自动创建
    /// </summary>
    private static void EnsureActiveSlot()
    {
        if (current_slot_id_ < 0)
        {
            SwitchToSlotById(CreateSaveSlot("新存档").slot_id_, false);
        }
    }

    /// <summary>
    /// 确保当前有激活的存档槽位（异步），没有则自动创建
    /// </summary>
    private static async Task EnsureActiveSlotAsync()
    {
        if (current_slot_id_ < 0)
        {
            SaveSlotInfo new_slot = await CreateSaveSlotAsync("新存档");
            await SwitchToSlotInternalAsync(new_slot.slot_id_);
        }
    }
    #endregion



    #region 辅助方法 — 内部槽位切换逻辑
    /// <summary>
    /// 内部槽位切换方法（同步）
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
    /// 加载指定槽位的数据到内存（同步）
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
    /// 加载指定槽位的数据到内存（异步）
    /// </summary>
    private static async Task SwitchToSlotInternalAsync(int slot_id)
    {
        current_slot_id_ = slot_id;
        slots_meta_.current_slot_id_ = slot_id;
        await SaveSlotsMetaToFileAsync();

        string slot_path = GetSlotFilePath(slot_id);

        if (File.Exists(slot_path))
        {
            try
            {
                string json = await Task.Run(() => File.ReadAllText(slot_path));
                if (!string.IsNullOrEmpty(json))
                {
                    root_json_object_ = await Task.Run(() => JObject.Parse(json));
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
        await SaveCurrentSlotToFileAsync();
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
    /// 将当前 root_json_object_ 写入当前槽位文件（同步）
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

        SaveSlotsMetaToFile();
    }

    /// <summary>
    /// 将当前 root_json_object_ 写入当前槽位文件（异步）
    /// </summary>
    private static async Task SaveCurrentSlotToFileAsync()
    {
        if (current_slot_id_ < 0) return;

        root_json_object_[VERSION_KEY] = Application.version;
        string json = root_json_object_.ToString(Formatting.Indented);
        string path = GetSlotFilePath(current_slot_id_);

        await Task.Run(() => File.WriteAllText(path, json));

        // 更新最后保存时间
        SaveSlotInfo current_slot = slots_meta_.slots_.Find(s => s.slot_id_ == current_slot_id_);
        if (current_slot != null)
        {
            current_slot.last_save_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        await SaveSlotsMetaToFileAsync();
    }

    /// <summary>
    /// 将槽位元数据写入文件（同步）
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
    /// 将槽位元数据写入文件（异步）
    /// </summary>
    private static async Task SaveSlotsMetaToFileAsync()
    {
        try
        {
            string json = await Task.Run(() =>
                JsonConvert.SerializeObject(slots_meta_, Formatting.Indented));
            string path = GetMetaFilePath();
            await Task.Run(() => File.WriteAllText(path, json));
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