using Godot;
using System;

public interface IStorable
{
    public void Discard(Vector3 position, Node parent);
}