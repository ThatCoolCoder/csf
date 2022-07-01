using Godot;
using System;
using System.Collections.Generic;

public class TomatoPlant : BasePlant
{
	public override List<GrowthStage> GrowthStages { get; set; } = new()
	{
		new(1, "Stage1"),
		new(2, "Stage2"),
		new(3, "Stage3"),
		new(3, new List<string>{"Stage3", "Fruit"}),
	};
}
