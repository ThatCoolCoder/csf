using Godot;
using System;

public class HidableCSGBox : CSGBox
{
	private void OnVisibilityChanged()
	{
		if (IsVisibleInTree()) UseCollision = true;
		else UseCollision = false;
	}
}
