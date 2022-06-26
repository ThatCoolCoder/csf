using Godot;
using System;
using System.Collections.Generic;

public class CubePlant : BasePlant
{
	public override List<GrowthStage> GrowthStages { get; set; } = new()
	{
		new(1, "Small"),
		new(2, "FullSize"),
		new(1, new List<String> {"Fruit", "FullSize"})
	};
}
