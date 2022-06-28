using Godot;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class PersistedGame
{
    [JsonProperty("S")]
    public int SaveFormatVersion { get; set; }
    [JsonProperty("T")]
    public DateTime SavedAtUtc { get; set; }
    [JsonProperty("N")]
    public List<PersistedNode> Nodes { get; set; }
}