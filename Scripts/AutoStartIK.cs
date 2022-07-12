using Godot;
using System;

public class AutoStartIK : SkeletonIK
{
    public override void _Ready()
    {
        Start();
    }
}