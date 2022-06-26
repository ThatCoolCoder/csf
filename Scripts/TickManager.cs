using Godot;
using System;
using System.Linq;

public class TickManager : Spatial
{
	public static TickManager Instance;
	public static readonly float SecondsPerSubTick = 1;
	public static readonly float SubTicksPerTick = 5;
	public static readonly float SubTicksPerDay = 600;
	public static readonly float TicksPerDay = 60;
	private Timer tickTimer;

	private int subTickCounter = 0;

	public override void _Ready()
	{
		Instance = this;
		tickTimer = GetNode<Timer>("TickTimer");
		tickTimer.WaitTime = SecondsPerSubTick;
		GetTree().GetNodesInGroup("Tickable").Cast<ITickable>().ToList().ForEach(x => x.SubTick());
	}

	private void SubTick()
	{
		subTickCounter += 1;
		subTickCounter %= (int) SubTicksPerTick;

		GetTree().GetNodesInGroup("Tickable").Cast<ITickable>().ToList().ForEach(x => x.SubTick());
		if (subTickCounter == 0) GetTree().GetNodesInGroup("Tickable").Cast<ITickable>().ToList().ForEach(x => x.Tick());
	}
}
