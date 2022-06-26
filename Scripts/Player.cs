using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public enum PlayerState
{
	OnGround,
	FreeFall
}

public class Player : KinematicBody
{
	// 1st person player controller
	
	// Movement exports
	[Export] private float sprintMultiplier = 2.0f;
	[Export] private float maxWalkSpeed = 10;
	[Export] private float walkAcceleration = 20;
	[Export] private float walkFriction = 40;
	[Export] private float jumpSpeed = 20;
	
	// Viewing exports
	[Export] private Vector2 mouseSensitivity = Vector2.One * 0.002f;
	[Export] private float maxLookDown = -1.2f;
	[Export] private float maxLookUp = 1.2f;

	// State management
	private Dictionary<PlayerState, Func<float, Vector3>> stateActions = new();
	public PlayerState State = PlayerState.OnGround;
	private Vector3 velocity;
	private Vector3 previousVelocity; // velocity last frame
	private float gravity = (float) ProjectSettings.GetSetting("physics/3d/default_gravity");

	// Node references
	private Spatial head;
	private AnimationPlayer animationPlayer;
	private Area harvestingArea;
	private RayCast viewRayCast;

	// Temp
	private CubePlantSeed seeds = new();

	public override void _Ready()
	{
		Input.SetMouseMode(Input.MouseMode.Captured);
		head = GetNode<Spatial>("Head");
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		animationPlayer.CurrentAnimation = "head_bobbing";
		harvestingArea = GetNode<Area>("HarvestingArea");
		viewRayCast = GetNode<RayCast>("Head/ViewRayCast");

		// Set here because we can't do it in field initialisers
		stateActions = new()
		{
			{PlayerState.OnGround, delta => OnGroundAction(delta)},
			{PlayerState.FreeFall, delta => FreeFallAction(delta)}
		};
	}

	public override void _Input(InputEvent _event)
	{
		// Rotate view with mouse
		if (_event is InputEventMouseMotion && Input.GetMouseMode() == Input.MouseMode.Captured)
		{
			var motionEvent = _event as InputEventMouseMotion;
			RotateY(-motionEvent.Relative.x * mouseSensitivity.x);
			head.RotateX(motionEvent.Relative.y * mouseSensitivity.y);
			head.Rotation = new Vector3(Mathf.Clamp(head.Rotation.x, -maxLookUp, -maxLookDown),
				head.Rotation.y, head.Rotation.z);
		}
	}

	public override void _PhysicsProcess(float delta)
	{
		var acceleration = stateActions[State](delta);
		acceleration.y -= gravity;
		velocity += acceleration * delta;
		velocity = MoveAndSlide(velocity, Vector3.Up);
		HandleSlideCollisions();
		
		UpdateAnimation();
		previousVelocity = velocity;
	}

	private void ResetState()
	{
		State = IsOnFloor() ? PlayerState.OnGround : PlayerState.FreeFall;
	}
	
	private void UpdateAnimation()
	{
		if (velocity.LengthSquared() < 0.01 || ! IsOnFloor())
		{
			animationPlayer.PlaybackActive = false;
			animationPlayer.Advance(0);
		}
		else
		{
			animationPlayer.PlaybackActive = true;
		}
		
	}

	private void HandleSlideCollisions()
	{
		for (int i = 0; i < GetSlideCount(); i ++) HandleCollision(GetSlideCollision(i));
	}

	private void HandleCollision(KinematicCollision collision)
	{
		// if (collision.Collider is Spatial)
		// {
		// 	var collider = (Spatial) collision.Collider;
		// }
	}

	#region Actions
	private Vector3 OnGroundAction(float delta)
	{
		ResetState();

		var acceleration = CheckWalkKeybinds();
		CheckJumpKeybinds();
		CheckHarvestKeybinds();
		CheckInventoryKeybinds();
		ClampHorizontalVelocity();
		FeelFloorFriction(acceleration, delta);
		CheckViewKeybinds();
		return acceleration;
	}

	private Vector3 FreeFallAction(float delta)
	{
		ResetState();
		
		var acceleration = CheckWalkKeybinds();
		ClampHorizontalVelocity();
		CheckViewKeybinds();

		return acceleration;
	}

	#endregion Actions

	#region UsedByActions
	private Vector3 CheckWalkKeybinds()
	{
		var acceleration = Vector3.Zero;
		if (Input.IsActionPressed("walk_forward")) acceleration.z += walkAcceleration;
		if (Input.IsActionPressed("walk_backward")) acceleration.z -= walkAcceleration;
		if (Input.IsActionPressed("walk_left")) acceleration.x += walkAcceleration;
		if (Input.IsActionPressed("walk_right")) acceleration.x -= walkAcceleration;
		acceleration = acceleration.Rotated(Vector3.Up, Rotation.y);

		return acceleration;
	}

	private void CheckViewKeybinds()
	{
		if (Input.IsActionJustPressed("toggle_mouse_lock"))
		{
			if (Input.GetMouseMode() == Input.MouseMode.Captured) Input.SetMouseMode(Input.MouseMode.Visible);
			else Input.SetMouseMode(Input.MouseMode.Captured);
		}
	}

	private void CheckJumpKeybinds()
	{
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.y = jumpSpeed;
			State = PlayerState.FreeFall;
		}
	}

	private void CheckHarvestKeybinds()
	{
		if (Input.IsActionJustPressed("harvest"))
		{
			var nodes = harvestingArea.GetOverlappingAreas().Cast<Spatial>().Where(x => x.IsInGroup("Harvestable"));
			foreach (var node in nodes) node.QueueFree();
		}
	}

	private void CheckInventoryKeybinds()
	{
		if (Input.IsActionJustPressed("discard"))
		{
			viewRayCast.ForceRaycastUpdate();
			if (viewRayCast.IsColliding())
			{
				seeds.Discard(viewRayCast.GetCollisionPoint(), GetParent());
			}
		}
	}

	private void FeelFloorFriction(Vector3 acceleration, float delta)
	{
		var rotatedAcceleration = acceleration.Rotated(Vector3.Up, Rotation.y);
		FeelFloorFriction(rotatedAcceleration.x == 0, rotatedAcceleration.z == 0, delta);
	}

	private void FeelFloorFriction(bool frictionX, bool frictionZ, float delta)
	{
		var rotatedVelocity = velocity.Rotated(Vector3.Up, Rotation.y);
		if (frictionX) rotatedVelocity.x = Utils.ConvergeValue(rotatedVelocity.x, 0, walkFriction * delta);
		if (frictionZ) rotatedVelocity.z = Utils.ConvergeValue(rotatedVelocity.z, 0, walkFriction * delta);
		velocity = rotatedVelocity.Rotated(Vector3.Up, -Rotation.y);
	}

	private void ClampHorizontalVelocity()
	{
		var currentSprintMultiplier = Input.IsActionPressed("sprint") ? sprintMultiplier : 1.0f;
		var horizontalVelocity = new Vector2(velocity.x, velocity.z);
		horizontalVelocity = horizontalVelocity.Clamped(maxWalkSpeed * currentSprintMultiplier);
		velocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.y);
	}

	#endregion UsedByActions
}
