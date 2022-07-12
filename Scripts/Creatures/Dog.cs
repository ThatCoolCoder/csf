using Godot;
using System;
using System.Collections.Generic;

public class Dog : Spatial
{
	public override void _Process(float delta)
	{
		var velocity = new Vector3();
		if (Input.IsKeyPressed((int) KeyList.I)) velocity.z += 8 * delta;
		if (Input.IsKeyPressed((int) KeyList.K)) velocity.z -= 8 * delta;
		if (Input.IsKeyPressed((int) KeyList.J)) Rotation = new Vector3(Rotation.x, Rotation.y + Mathf.Pi * delta / 2, Rotation.z);
		if (Input.IsKeyPressed((int) KeyList.L)) Rotation = new Vector3(Rotation.x, Rotation.y - Mathf.Pi * delta / 2, Rotation.z);
		velocity = velocity.Rotated(Vector3.Up, Rotation.y);
		Translation += velocity;
	}
}
