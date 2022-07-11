using Godot;
using System;

public interface IStorable
{
    public float Mass { get; } // (kilograms)
    public float Volume { get; } // (litres)
    public void Discard(Vector3 position, Node parent);
}