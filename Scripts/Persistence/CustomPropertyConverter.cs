using Godot;
using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CustomPropertyConverter : JsonConverter<CustomProperty>
{
    public override CustomProperty ReadJson(JsonReader reader, Type objectType, CustomProperty existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.ReadFrom(reader) as JObject;
        var typeName = obj.GetValue("T").ToString();
        var type = Type.GetType(typeName);
        return new CustomProperty(typeName, obj.GetValue("N").ToString(), obj.GetValue("V").ToObject(type));
    }

    public override void WriteJson(JsonWriter writer, CustomProperty value, JsonSerializer serializer)
    {
        throw new NotImplementedException("This converter doesn't need to write");
    }

    public override bool CanWrite { get { return false; } }
}