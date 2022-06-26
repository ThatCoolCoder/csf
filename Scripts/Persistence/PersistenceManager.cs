// Manages saving/loading of entities

// To make your own nodes persistable, add them to the Persistable group.
// Persistable nodes can exist in the base scene from the start or can have been instantiated in gameplay - the manager will detect whether an instantiated node is needed
// Transforms of Spatial-derived nodes are automatically persisted.
// To perist custom properties, mark them with the [PersistableProperty] attribute. (properties must be public)
// To save file size, it's recommended to use the Newtonsoft.Json [JsonProperty] attribute to shorten property names

using Godot;
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

public class PersistenceManager
{
    public static readonly string TestFileName = "user://game.bson";

    public static void PersistScene(SceneTree tree, string fileName)
    {
        // Persist an entire scene
        var persistableNodes = tree.GetNodesInGroup("Persistable").Cast<Node>().ToList();
        JArray result = new();
        foreach (var node in persistableNodes) result.Add(PersistNode(node));
        
        MemoryStream binaryResult = new MemoryStream();
        using var writer = new BsonDataWriter(binaryResult);
        result.WriteTo(writer);

        using var file = new Godot.File();
        file.Open(fileName, Godot.File.ModeFlags.Write);
        file.StoreBuffer(binaryResult.ToArray());
    }

    public static async Task LoadPersistedScene(SceneTree tree, string fileName, string baseScenePath)
    {
        // Load scene from baseScenePath then load persisted data

        // Read file
        var file = new Godot.File();
        file.Open(fileName, Godot.File.ModeFlags.Read);
        var binaryData = file.GetBuffer();
        file.Close();

        // Load the scene
        tree.ChangeScene(baseScenePath);
        await tree.ToSignal(tree, "idle_frame");

        // Load the data
        using var ms = new MemoryStream(binaryData);
        using var reader = new BsonDataReader(ms);
        var persistedData = JArray.ReadFrom(reader) as JArray;
        GD.Print(ms.Length);
        GD.Print(persistedData.GetType());
        foreach (var token in persistedData) LoadPersistedNode((JObject) token, tree);
    }

    private static void LoadPersistedNode(JObject savedNode, SceneTree tree)
    {
        var debugPath = savedNode.GetValue("Path").ToString();
        GD.Print($"Loading: {debugPath}");

        // Instance it
        var typeString = savedNode.GetValue("Type").ToString();
        var type = Type.GetType(typeString);

        var node = tree.Root.GetNode(savedNode.GetValue("Path").ToString());
        // If node was created after scene initialization, create it now
        if (node == null)
        {
            var prefab = ResourceLoader.Load<PackedScene>(savedNode.GetValue("Filename").ToString());
            node = prefab.Instance();

            // Find parent
            var nodePath = new NodePath(savedNode.GetValue("Path").ToString());
            var nameList = nodePath.ToNameList();
            var parentPath = NodePathExtensions.FromList(nameList.Take(nameList.Count() - 1).ToList());
            var parent = tree.Root.GetNode(parentPath);

            // Add to parent
            parent.AddChild(node, true);
        }

        // Set properties
        if (node is Spatial spatial)
        {
            spatial.Transform = savedNode.GetValue("Transform").ToObject<Transform>();
        }
        // Custom properties
        foreach (var customProperty in (savedNode.GetValue("CustomProperties") as JObject))
        {
            var property = type.GetProperty(customProperty.Key);
            property.SetValue(node, customProperty.Value.ToObject(property.PropertyType));
        }
    }

    private static JToken PersistNode(Node node)
    {
        GD.Print($"Saving: {node.GetPath().ToString()}");

        var jObject = new JObject();
        jObject.Add("Filename", node.Filename);
        jObject.Add("Path", node.GetPath().ToString());
        jObject.Add("Type", node.GetType().Name);

        if (node is Spatial spatial)
        {
            // persist spatial information - transform, etc
            jObject.Add("Transform", JToken.FromObject(spatial.Transform));
        }
        
        // Persist custom properties
        var type = node.GetType();
        var properties = type.GetProperties()
            .Where(prop => prop.IsDefined(typeof(PersistableProperty), true));

        var customProperties = new JObject();
        foreach (var property in properties)
        {
            customProperties.Add(property.Name, JToken.FromObject(property.GetValue(node)));
        }
        jObject.Add("CustomProperties", customProperties);

        return jObject;
    }
}