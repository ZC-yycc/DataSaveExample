using Newtonsoft.Json;
using System;

public class TestSaveData
{
    public int                                           test_int_ = 0;
    public string                                        test_string_ = "";
}

public class TestConverter : SaveJsonConverterBase<TestSaveData>
{
    public override TestSaveData ReadJson(JsonReader reader, Type objectType, TestSaveData existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return serializer.Deserialize<TestSaveData>(reader);
    }

    public override void WriteJson(JsonWriter writer, TestSaveData value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}
