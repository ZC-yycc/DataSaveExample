using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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

public static class SaveManager
{
    private static JObject                              root_json_object_;
    private static readonly string                      save_file_path_;
    private static JsonSerializerSettings               serializer_settings_;
    private const string                                FILE_NAME = "game_save.json";   // 存档文件名

    //开发阶段使用字段
    private const string                                VERSION_KEY = "GameVersion";   // 版本检查字段


    static SaveManager()
    {
        save_file_path_ = Path.Combine(Application.persistentDataPath, FILE_NAME);
        Application.quitting += SaveToFile;   // 在应用退出时自动保存

        InitSerializerSettings();
        LoadOrInitWithVersionCheck();
    }



    
    #region 版本检查与初始化
    private static void InitSerializerSettings()
    {
        List<JsonConverter> types = ConverterHelper.GetConverterInstances();

        serializer_settings_ = new()
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,   // 忽略循环引用
            TypeNameHandling = TypeNameHandling.Auto,               // 支持多态类型
            Converters = types
        };
    }

    /// <summary>
    /// 初始化或加载存档文件
    /// </summary>
    private static void LoadOrInitWithVersionCheck()
    {
        // 检查存档文件是否存在
        if (!File.Exists(save_file_path_))
        {
            ResetSaveFile();
            return;
        }

        try
        {
            string json = File.ReadAllText(save_file_path_);
            if (string.IsNullOrEmpty(json))         // 如果 JSON 为空，则初始化
            {
                ResetSaveFile();
                return;
            }

            root_json_object_ = JObject.Parse(json);
            string saved_version = root_json_object_.Value<string>(VERSION_KEY);
            if (saved_version != Application.version)               // 如果版本不一致，则重置
            {
                Debug.Log("开发模式：由于版本变更，重置存档");
                ResetSaveFile();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            ResetSaveFile();
        }
    }
    #endregion





    #region 保存数据
    /// <summary>
    /// 保存数据到指定枚举类型的键
    /// </summary>
    public static void SaveData<T>(T obj, string key = null) where T : class
    {
        try
        {
            key ??= typeof(T).Name;
            string obj_json = JsonConvert.SerializeObject(obj, Formatting.Indented, serializer_settings_);
            JToken obj_node = JToken.Parse(obj_json);
            root_json_object_[key] = obj_node;
            SaveToFile();
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSON 序列化失败: {ex.Message}");
        }
    }
    public static void SaveData(object obj, string key)
    {
        try
        {
            string obj_json = JsonConvert.SerializeObject(obj, Formatting.Indented, serializer_settings_);
            JToken obj_node = JToken.Parse(obj_json);
            root_json_object_[key] = obj_node;
            SaveToFile();
        }
        catch (JsonException ex)
        {
            Debug.LogError($"JSON 序列化失败: {ex.Message}");
        }
    }
    #endregion





    #region 读取数据
    /// <summary>
    /// 从指定枚举类型的键读取数据
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
        catch(Exception e)
        {
            Debug.LogError($"读取{nameof(T)}数据失败：{e.Message}");
            return default;
        }
    }
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
    public static bool HasData<T>(string key = null) where T : class
    {
        key ??= typeof(T).Name;
        return root_json_object_.ContainsKey(key);
    }
    public static bool HasData(string key)
    {
        return root_json_object_.ContainsKey(key);
    }
    #endregion





    #region 删除数据
    public static void DeleteData<T>(string key = null) where T : class
    {
        key ??= typeof(T).Name;
        if (root_json_object_.Remove(key))
        {
            SaveToFile();
            Debug.Log($"已删除 {key} 类型的存档数据");
        }
        else
        {
            Debug.LogWarning($"未找到 {key} 类型的存档数据，无法删除");
        }
    }
    public static void DeleteData(string key)
    {
        if (root_json_object_.Remove(key))
        {
            SaveToFile();
            Debug.Log($"已删除 {key} 类型的存档数据");
        }
        else
        {
            Debug.LogWarning($"未找到 {key} 类型的存档数据，无法删除");
        }
    }
    public static void DeleteSaveData()
    {
        if (File.Exists(save_file_path_))
        {
            File.Delete(save_file_path_);
            Debug.Log("已删除存档");
        }
        else
        {
            Debug.Log("未找到存档");
        }
        LoadOrInitWithVersionCheck();
    }
    #endregion





    #region 辅助方法
    /// <summary>
    /// 重置存档
    /// </summary>
    private static void ResetSaveFile()
    {
        root_json_object_ = new JObject();
        SaveToFile();
    }
    /// <summary>
    /// 将 JObject 写入文件
    /// </summary>
    private static void SaveToFile()
    {
        root_json_object_[VERSION_KEY] = Application.version;
        string json = root_json_object_.ToString(Formatting.Indented); // 格式化输出
        File.WriteAllText(save_file_path_, json);
    }
    #endregion
}