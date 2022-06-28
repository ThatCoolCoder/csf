// Manages saving/loading of entities

// To make your own nodes persistable, add them to the Persistable group.
// Persistable nodes can exist in the base scene from the start or can have been instantiated in gameplay - the manager will detect whether an instantiated node is needed
// Transforms of Spatial-derived nodes are automatically persisted.
// To perist custom properties, mark them with the [PersistableProperty] attribute. (properties must be public)

// Internal notes:
// A lot of work has gone into minimising save-file size.
// Transforms are really big to store in object form in JSON but fortunately, Godot can base64-encode them by default and this makes a huge difference.
// We have custom one-character names for all the "framework" properties to try and minimise space.

using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class PersistedGame
{
    [JsonProperty("S")]
    public int SaveFormatVersion { get; set; }
    [JsonProperty("T")]
    public DateTime SavedAtUtc { get; set; }
    [JsonProperty("N")]
    public List<PersistedNode> Nodes { get; set; }
}

class PersistedNode
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

public class PersistenceManager
{
    public static readonly string TestFileName = "user://game.json";
    public static readonly int SaveFormatVersion = -1; // -1 indicates pre-alpha

    public static void PersistScene(SceneTree tree, string fileName, bool debug = false)
    {
        // Persist an entire scene
        var persistableNodes = tree.GetNodesInGroup("Persistable").Cast<Node>().ToList();
        var result = new PersistedGame()
        {
            SaveFormatVersion = SaveFormatVersion,
            SavedAtUtc = DateTime.UtcNow,
            Nodes = new()
        };
        foreach (var node in persistableNodes) result.Nodes.Add(PersistNode(node));
        
        var file = new File();
        file.Open(fileName, File.ModeFlags.Write);
        file.StoreString(JsonConvert.SerializeObject(result, debug ? Formatting.Indented : Formatting.None, new CustomPropertyConverter()));
        file.Close();
    }

    public static async Task LoadPersistedScene(SceneTree tree, string fileName, string baseScenePath)
    {
        // Load scene from baseScenePath then load persisted data
        var file = new File();
        file.Open(fileName, File.ModeFlags.Read);
        var stringData = file.GetAsText();
        file.Close();

        var scene = ResourceLoader.Load<PackedScene>(baseScenePath).Instance();

        // Manually swap scenes
        tree.ChangeScene(baseScenePath);
        await tree.ToSignal(tree, "idle_frame");

        // Load the data
        var persistedData = JsonConvert.DeserializeObject<PersistedGame>(stringData, new CustomPropertyConverter());
        if (persistedData.SaveFormatVersion != SaveFormatVersion)
            throw new Exception($"Error loading game: save format is incompatible. Saved in format {persistedData.SaveFormatVersion}, reading in format {SaveFormatVersion}");
        foreach (var persistedNode in persistedData.Nodes) LoadPersistedNode(persistedNode, tree);
    }

    private static PersistedNode PersistNode(Node node)
    {
        GD.Print($"Saving: {node.GetPath().ToString()}");

        var result = new PersistedNode()
        {
            Filename = node.Filename,
            Path = node.GetPath().ToString(),
            Type = node.GetType().Name,
            CustomProperties = new()
        };

        if (node is Spatial spatial)
        {
            // persist spatial information - transform, etc
            result.TransformString = TransformToString(spatial.Transform);
        }
        
        // Persist custom properties
        var type = node.GetType();
        var properties = type.GetProperties()
            .Where(prop => prop.IsDefined(typeof(PersistableProperty), true));

        foreach (var property in properties)
        {
            result.CustomProperties.Add(new(property.PropertyType.ToString(), property.Name, property.GetValue(node)));
        }

        return result;
    }

    private static void LoadPersistedNode(PersistedNode persistedNode, SceneTree tree)
    {
        GD.Print($"Loading: {persistedNode.Path}");

        // Add it to scene
        var type = Type.GetType(persistedNode.Type);
        var node = tree.Root.GetNode(persistedNode.Path);
        Node parent = null;
        bool nodeInstantiated = false;
        // If node was created after scene initialization, create it now
        if (node == null)
        {
            var prefab = ResourceLoader.Load<PackedScene>(persistedNode.Filename);
            node = prefab.Instance();

            // Find parent
            var nodePath = new NodePath(persistedNode.Path);
            var nameList = nodePath.ToNameList();
            var parentPath = NodePathExtensions.FromList(nameList.Take(nameList.Count() - 1).ToList());
            parent = tree.Root.GetNode(parentPath);
            nodeInstantiated = true;
        }

        // Set properties
        if (node is Spatial spatial)
        {
            spatial.Transform = StringToTransform(persistedNode.TransformString);
        }
        // Custom properties
        foreach (var customProperty in persistedNode.CustomProperties)
        {
            var property = type.GetProperty(customProperty.Name);
            property.SetValue(node, customProperty.Value);
        }

        // If node needs to be added to scene, do so now
        if (nodeInstantiated)
        {
            parent.AddChild(node, true);
        }
    }

    private static string TransformToString(Transform transform)
    {
        return Marshalls.VariantToBase64(transform);
    }

    private static Transform StringToTransform(string str)
    {
        return (Transform) Marshalls.Base64ToVariant(str);
    }
}