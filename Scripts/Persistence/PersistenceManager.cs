// Manages saving/loading of entities

using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class PersistenceManager
{
    public static readonly string TestFileName = "user://game.json";

    public static void PersistScene(SceneTree tree, string fileName)
    {
        // Persist an entire scene
        var persistableNodes = tree.GetNodesInGroup("Persistable").Cast<Node>().ToList();
        JArray result = new();
        foreach (var node in persistableNodes) result.Add(PersistNode(node));
        
        var file = new File();
        file.Open(fileName, File.ModeFlags.Write);
        file.StoreString(result.ToString());
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
        var persistedData = JArray.Parse(stringData);
        foreach (var token in persistedData) LoadPersistedNode((JObject) token, tree);
    }

    private static void LoadPersistedNode(JObject savedNode, SceneTree tree)
    {
        var debugPath = savedNode.GetValue("Path").ToString();
        GD.Print($"Loading: {debugPath}");

        // Instance it
        var typeString = savedNode.GetValue("Type").ToString();
        var type = Type.GetType(typeString); 
        var scene = ResourceLoader.Load<PackedScene>(savedNode.GetValue("Filename").ToString());
        var instance = scene.Instance();

        // Find parent
        var nodePath = new NodePath(savedNode.GetValue("Path").ToString());
        var nameList = nodePath.ToNameList();
        var parentPath = NodePathExtensions.FromList(nameList.Take(nameList.Count() - 1).ToList());
        var parent = tree.Root.GetNode(parentPath);

        // Set properties
        if (instance is Spatial spatial)
        {
            spatial.Transform = savedNode.GetValue("Transform").ToObject<Transform>();
        }
        // Custom properties
        foreach (var customProperty in (savedNode.GetValue("CustomProperties") as JObject))
        {
            var property = type.GetProperty(customProperty.Key);
            GD.Print(property.Name, customProperty.Value.ToObject(property.PropertyType));
            property.SetValue(instance, customProperty.Value.ToObject(property.PropertyType));
        }

        // Add to scene
        parent.AddChild(instance, true);
    }

    private static JToken PersistNode(Node node)
    {
        var jObject = new JObject();
        jObject.Add("Filename", node.Filename);
        jObject.Add("Path", node.GetPath().ToString());
        jObject.Add("Type", node.GetType().Name);
        GD.Print($"Saving: {node.GetPath().ToString()}");
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