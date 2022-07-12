using Godot;
using System;

public class LimbController : Spatial
{
	// Controls limb in procedural walk cycle.
	// Move to position of start of step in editor
	// Add a child node called StepEndPosition for randomising stuff.

	[Export] private float maxStepLength = 1; // Step when foot has moved this far
	private Vector3 stepStartOffset; // Offset for position of new steps
	private Vector3 stepStartPosition; // Position where the current step was started
	private Vector3 stepTargetPosition;
	private Vector3 lastPlantedPosition;
	[Export] private float stepDuration = 1;
	[Export] private float footRaising = 1; // feet are raised this high during a step
	private float timeSinceLastPlanted;
	private State state = State.Planted;

	private enum State
	{
		Stepping,
		Planted
	}

	public override void _Ready()
	{
		stepStartOffset = Translation;

		SetAsToplevel(true); // Makes positions global not local
	}

	public override void _PhysicsProcess(float delta)
	{
		if (state == State.Planted)
		{
			var nextStepPosition = CalcStepTargetPosition();
			nextStepPosition.y = 0;
			var flatPosition = Translation;
			flatPosition.y = 0;
			
			if (flatPosition.DistanceSquaredTo(nextStepPosition) > maxStepLength * maxStepLength)
			{
				nextStepPosition.y = Translation.y;
				stepTargetPosition = nextStepPosition;
				state = State.Stepping;
			}
			timeSinceLastPlanted = 0;
			lastPlantedPosition = Translation;
		}
		else
		{
			timeSinceLastPlanted += delta;
			stepTargetPosition = CalcStepTargetPosition();
			stepTargetPosition.y = lastPlantedPosition.y;

			var stepProportion = timeSinceLastPlanted / stepDuration;
			
			// Raise feet when they are in air
			float footYPos = 0;
			if (stepProportion < .5)
			{
				footYPos = Utils.Slerp(lastPlantedPosition.y, lastPlantedPosition.y + footRaising, stepProportion * 2);
			}
			else
			{
				footYPos = Utils.Slerp(lastPlantedPosition.y + footRaising, stepTargetPosition.y, stepProportion * 2 - 1);
			}

			var footPosition = Utils.Slerp(lastPlantedPosition, stepTargetPosition, stepProportion);
			footPosition.y = footYPos;
			Translation = footPosition;

			if (stepProportion > 1)
			{
				state = State.Planted;
			}
		}
	}

	private Vector3 CalcStepTargetPosition()
	{
		return GetParent<Spatial>().ToGlobal(stepStartOffset);
	}
}
