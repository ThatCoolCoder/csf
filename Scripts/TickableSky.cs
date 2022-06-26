using Godot;
using System;

public class TickableSky : Sky, ITickable
{

	public void SubTick()
	{
		TimeOfDay = (TimeOfDay + 1 / TickManager.SubTicksPerDay) % 1.0f;
	}

	public void Tick()
	{

	}
}
