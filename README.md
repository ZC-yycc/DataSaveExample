# DataSaveExample — 多存档系统

一个面向 **Unity** 的轻量级多存档解决方案，基于 Newtonsoft.Json 实现数据的序列化与持久化。支持多槽位管理、同步/异步 API、旧版存档自动迁移，以及可扩展的自定义 JSON 转换器。

---

## 核心特性

- **多存档槽位** — 创建、删除、切换、重命名、列举任意数量的存档槽位
- **同步 & 异步 API** — 所有操作均提供同步和 `async Task` 双版本，灵活适配不同场景
- **自动迁移** — 检测旧版单存档文件并自动迁移到新槽位系统
- **版本感知** — 游戏版本变更时自动重置存档，避免反序列化兼容问题
- **类型安全读写** — 泛型 `SaveData<T>` / `ReadData<T>` + 自定义 key，支持嵌套复杂类型
- **可扩展转换器** — 继承 `SaveJsonConverterBase<T>`，由 `ConverterHelper` 自动扫描注册
- **退出保护** — `Application.quitting` 时同步写入，确保数据不丢失
- **格式化 JSON** — 存档文件使用 `Formatting.Indented`，便于调试和版本控制

---

## 项目结构

```
Assets/
├── SaveSystem/
│   ├── SaveManager.cs          # 核心存档管理器（静态类）
│   ├── ConverterHelper.cs      # 自定义 JsonConverter 自动发现与注册
│   └── Converters/
│       └── TestConverter.cs    # 自定义转换器示例
└── TestScripts/
    └── PlayerData.cs           # 可序列化数据类示例
```

存档文件存放于 `Application.persistentDataPath`：

| 文件 | 用途 |
|---|---|
| `save_slots_meta.json` | 所有槽位的元数据（ID、名称、创建时间、最后保存时间、当前激活槽位） |
| `save_slot_{id}.json` | 单个槽位的实际游戏数据（JObject 键值结构） |
| `game_save.json` | 旧版单存档文件（迁移后自动删除） |

---

## 快速开始

### 1. 初始化

静态构造函数自动完成初始化，无需手动调用。如需异步初始化：

```csharp
await SaveManager.InitializeAsync();
```

### 2. 创建存档槽位并写入数据

```csharp
// 创建第一个槽位（自动激活）
SaveSlotInfo slot = SaveManager.CreateSaveSlot("冒险之旅");

// 保存数据（key 默认为类型名）
PlayerData playerData = new PlayerData { player_name_ = "勇者", level_ = 10 };
SaveManager.SaveData(playerData);

// 保存数据（自定义 key）
SaveManager.SaveData(playerData, "player_info");
```

### 3. 读取数据

```csharp
// 泛型读取
PlayerData loaded = SaveManager.ReadData<PlayerData>();

// 按 key 读取
object data = SaveManager.ReadData("player_info", typeof(PlayerData));

// 安全读取
if (SaveManager.TryReadData<PlayerData>(out PlayerData result))
{
    Debug.Log($"玩家名称: {result.player_name_}");
}
```

### 4. 槽位管理

```csharp
// 创建新槽位
SaveSlotInfo slot2 = SaveManager.CreateSaveSlot("二周目");

// 切换槽位
SaveManager.SwitchToSaveSlot(slot2.slot_id_);

// 重命名
SaveManager.RenameCurrentSlot("困难模式");

// 获取所有槽位
List<SaveSlotInfo> allSlots = SaveManager.GetAllSaveSlots();
foreach (var s in allSlots)
{
    Debug.Log($"[{s.slot_id_}] {s.slot_name_} - 最后保存: {s.last_save_time}");
}

// 删除槽位
SaveManager.DeleteSaveSlot(slot2.slot_id_);

// 删除所有
SaveManager.DeleteAllSaveSlots();
```

### 5. 数据检查与删除

```csharp
// 检查是否存在
bool hasPlayer = SaveManager.HasData<PlayerData>();

// 删除指定数据
SaveManager.DeleteData<PlayerData>();
SaveManager.DeleteData("player_info");

// 清空当前槽位全部数据
SaveManager.DeleteSaveData();
```

---

## API 参考

### 槽位管理

| 方法 | 说明 |
|---|---|
| `CreateSaveSlot(string name)` | 创建新槽位，首个槽位自动激活 |
| `DeleteSaveSlot(int id)` | 删除槽位及其文件，若为当前槽位则重置激活状态 |
| `SwitchToSaveSlot(int id, bool saveCurrent = true)` | 切换到指定槽位，可选择是否先保存当前 |
| `RenameCurrentSlot(string newName)` | 重命名当前激活槽位 |
| `DeleteAllSaveSlots()` | 删除所有槽位并重建默认 |
| `GetCurrentSlotInfo()` | 获取当前激活槽位信息 |
| `GetAllSaveSlots()` | 获取所有槽位列表（副本） |
| `GetSaveSlotCount()` | 获取槽位数量 |
| `GetSlotInfo(int id)` | 根据 ID 获取槽位信息 |
| `CurrentSlotId` | 当前激活槽位 ID（属性） |

### 数据读写（同步）

| 方法 | 说明 |
|---|---|
| `SaveData<T>(T obj, string key = null)` | 保存泛型对象，key 默认类型名 |
| `SaveData(object obj, string key)` | 保存对象到指定 key |
| `ReadData<T>(string key = null)` | 泛型读取，未找到返回 `default` |
| `ReadData(string key, Type type)` | 按 key 和 Type 读取 |
| `TryReadData<T>(out T data, string key = null)` | 安全泛型读取 |
| `TryReadData(string key, Type type, out object data)` | 安全按类型读取 |
| `HasData<T>(string key = null)` | 检查是否存在泛型对应数据 |
| `HasData(string key)` | 检查 key 是否存在 |
| `DeleteData<T>(string key = null)` | 删除指定数据 |
| `DeleteData(string key)` | 按 key 删除 |
| `DeleteSaveData()` | 清空当前槽位 |

> 所有上述方法均有对应的 `Async` 后缀版本。

### 属性与事件

| 成员 | 说明 |
|---|---|
| `IsBusy` | 是否有异步操作进行中 |
| `CurrentSlotId` | 当前激活槽位 ID |

---

## 自定义转换器

当默认 JSON 序列化无法满足需求时（如循环引用、特殊类型），可自定义转换器：

```csharp
// 1. 继承 SaveJsonConverterBase<T>
public class MyCustomConverter : SaveJsonConverterBase<MySpecialType>
{
    public override MySpecialType ReadJson(JsonReader reader, Type objectType,
        MySpecialType existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // 自定义反序列化逻辑
        return base.ReadJson(reader, objectType, existingValue, hasExistingValue, serializer);
    }

    public override void WriteJson(JsonWriter writer, MySpecialType value,
        JsonSerializer serializer)
    {
        // 自定义序列化逻辑
        base.WriteJson(writer, value, serializer);
    }
}
```

`ConverterHelper` 通过反射自动扫描所有非系统程序集中继承 `SaveJsonConverterBase<T>` 的叶子类型，并在 `SaveManager` 初始化时注册到 `JsonSerializerSettings.Converters`，无需手动配置。

---

## 存档文件格式

### 元数据 (`save_slots_meta.json`)

```json
{
  "current_slot_id_": 0,
  "slots_": [
    {
      "slot_id_": 0,
      "slot_name_": "冒险之旅",
      "create_time_": "2026-05-07 18:00:00",
      "last_save_time": "2026-05-07 18:30:00"
    }
  ]
}
```

### 数据文件 (`save_slot_0.json`)

```json
{
  "GameVersion": "1.0.0",
  "PlayerData": {
    "player_name_": "勇者",
    "player_id_": 1,
    "level_": 10,
    "score_": 500,
    "health_": 85.0,
    "is_alive_": true
  }
}
```

---

## 使用场景

- **RPG 多存档** — 玩家保存多个进度的冒险旅程
- **解谜游戏** — 不同关卡分支分别存档
- **调试/测试** — 开发期间在不同状态间快速切换
- **多用户** — 单设备上允许多人轮流游玩各自存档

---

## 依赖

- **Unity** 2022.3 或更高版本
- **Newtonsoft.Json** (通过 Unity Package Manager 安装 `com.unity.nuget.newtonsoft-json`)

---

## 注意事项

- 静态构造函数在首次访问 `SaveManager` 时执行，因此**无需场景中的 GameObject 绑定**
- 异步操作在执行期间 `IsBusy` 为 `true`，避免并发操作冲突
- 游戏退出时自动同步保存当前槽位数据，无需手动监听 `Application.quitting`
- 自定义转换器的程序集不能以 `System`、`Unity`、`mscorlib`、`netstandard` 开头，否则会被扫描跳过
- 旧版存档文件 `game_save.json` 会在迁移后自动删除，迁移的槽位默认命名为「迁移的存档」

---

## License

MIT