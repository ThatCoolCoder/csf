using Godot;
using System;
using Newtonsoft.Json;

// Used internally inside the persistence manager

public class CustomProperty
{
    [JsonProperty("T")]
    public string Type { get; set; }
    [JsonProperty("N")]
    public string Name { get; set; }
    [JsonProperty("V")]
    public object Value { get; set; }

    public CustomProperty(string type, string name, object value)
    {
        Type = type;
        Name = name;
        Value = value;
    }
}