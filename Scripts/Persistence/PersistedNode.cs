using Godot;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class PersistedNode
{
    [JsonProperty("F")]
    public string Filename { get; set; }
    [JsonProperty("P")]
    public string Path { get; set; }
    [JsonProperty("T")]
    public string Type { get; set; }
    [JsonProperty("R")]
    public string TransformString { get; set; } // Much more efficient to base64-encode a transform than store it as a whole json
    [JsonProperty("C")]
    public List<CustomProperty> CustomProperties;
}