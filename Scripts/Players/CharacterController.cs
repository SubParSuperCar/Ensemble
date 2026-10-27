using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Utils;
using Godot;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Players;

[GlobalClass]
public partial class CharacterController : CharacterBody3D
{
	// Godot's own, which IsOnFloor allows beyond FloorMaxAngle
	private const float FloorAngleThreshold = 0.01f;

	private static readonly StringName JumpAction = "character_jump";
	private static readonly StringName RunAction = "character_run";
	private static readonly StringName MoveLeftAction = "character_move_left";
	private static readonly StringName MoveRightAction = "character_move_right";
	private static readonly StringName MoveForwardAction = "character_move_forward";
	private static readonly StringName MoveBackwardAction = "character_move_backward";

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m/s")]
	public float WalkSpeed { get; set; } = 6f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m/s")]
	public float RunSpeed { get; set; } = 16f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float JumpHeight { get; set; } = 1.25f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float TurnRate { get; set; } = 11.25f;

	[Export(PropertyHint.Range, "-1,0,or_greater,hide_slider")]
	public float FirstPersonInvisibleProximityThreshold { get; set; } = 1f;

	// A kinematic body has no weight of its own, so it presses on whatever simulated body it stands on
	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:kg")]
	public float Mass { get; set; } = 70f;

	[Export] public Camera3D Camera { get; set; } = null!;
	[Export] public Node3D Terrain { get; set; } = null!; // Terrain3D, which has no C# bindings

	public override void _Ready()
	{
		PhysicsServer3D.BodySetEnableContinuousCollisionDetection(GetRid(), true);

		Camera.MakeCurrent();

		// Terrain3D centers its mesh on its camera, and stops looking for one if there is none when it first looks
		// (e.g., while a client joins), so it is handed the view camera
		if (Terrain?.IsClass("Terrain3D") is true)
			Terrain.Call("set_camera", Camera);
	}

	public override void _PhysicsProcess(double delta)
	{
		Visible = Camera.GlobalPosition.DistanceTo(GlobalPosition) > FirstPersonInvisibleProximityThreshold;

		var velocity = Velocity;

		if (!IsOnFloor())
			velocity += GetGravity() * (float)delta;
		else if (!InputSink.IsSunk && Input.IsActionPressed(JumpAction))
			velocity.Y = MathF.Sqrt(2 * JumpHeight * -GetGravity().Y);

		var inputDirection = InputSink.IsSunk
			? Vector2.Zero
			: Input.GetVector(MoveLeftAction, MoveRightAction, MoveForwardAction, MoveBackwardAction);

		if (inputDirection != Vector2.Zero)
		{
			var cameraYaw = Camera.GlobalRotation.Y;
			var moveDirection = new Vector3(inputDirection.X, 0, inputDirection.Y).Rotated(Vector3.Up, cameraYaw);

			var speed = Input.IsActionPressed(RunAction) ? RunSpeed : WalkSpeed;
			velocity.X = moveDirection.X * speed;
			velocity.Z = moveDirection.Z * speed;

			var rotation = Rotation;
			var turnAngle = MathF.Atan2(moveDirection.X, moveDirection.Z);
			rotation.Y = Mathf.LerpAngle(rotation.Y, turnAngle, Smoothing.GetWeight(TurnRate, delta));

			Rotation = rotation;
		}
		else
		{
			velocity.X = 0;
			velocity.Z = 0;
		}

		Velocity = velocity;
		MoveAndSlide();

		PressOnFloor();
	}

	private void PressOnFloor()
	{
		if (!IsOnFloor())
			return;

		for (var i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);

			if (
				collision.GetCollider() is not RigidBody3D { Freeze: false } body ||
				!IsFloorNormal(collision.GetNormal()))
				continue;

			body.ApplyForce(GetGravity() * Mass, collision.GetPosition() - body.GlobalPosition);
			return;
		}
	}

	private bool IsFloorNormal(Vector3 normal) => normal.AngleTo(UpDirection) <= FloorMaxAngle + FloorAngleThreshold;
}
