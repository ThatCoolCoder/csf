using Godot;
using System;

public class BaseSeed : IStorable
{
    private PackedScene prefab;
    private RandomNumberGenerator random = new();

    public BaseSeed(string prefabPath)
    {
        prefab = ResourceLoader.Load<PackedScene>(prefabPath);
    }

    public BaseSeed(PackedScene _prefab)
    {
        prefab = _prefab;
    }

    public void Discard(Vector3 position, Node parent)
    {
        var plant = prefab.Instance<Spatial>();
        plant.Translation = position;
        plant.RotationDegrees = new Vector3(0, random.RandfRange(0, 360), 0);
        parent.AddChild(plant);
    }
}