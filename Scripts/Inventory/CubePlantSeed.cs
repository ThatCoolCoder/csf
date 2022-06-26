using Godot;
using System;

public class CubePlantSeed : IStorable
{
    private PackedScene prefab = ResourceLoader.Load<PackedScene>("res://Scenes/Plants/CubePlant.tscn");

    public void Discard(Vector3 position, Node parent)
    {
        var plant = prefab.Instance<Spatial>();
        plant.Translation = position;
        parent.AddChild(plant);
    }
}