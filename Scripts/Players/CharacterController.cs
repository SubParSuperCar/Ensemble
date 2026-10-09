using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Utils;
using Godot;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Players;

[GlobalClass]
public partial class CharacterController : CharacterBody3D
{
	private static readonly StringName JumpAction = "char_jump";
	private static readonly StringName RunAction = "char_run";
	private static readonly StringName StrafeLeftAction = "char_strafe_left";
	private static readonly StringName StrafeRightAction = "char_strafe_right";
	private static readonly StringName MoveForwardAction = "char_move_forward";
	private static readonly StringName MoveBackwardAction = "char_move_backward";

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

	[Export] public Camera3D Camera { get; set; } = null!;
	[Export] public Node3D Terrain { get; set; } = null!; // Terrain3D, which has no C# bindings

	public override void _Ready()
	{
		PhysicsServer3D.BodySetEnableContinuousCollisionDetection(GetRid(), true);

		// Terrain3D only generates collision around its camera, so this probe camera keeps it under the body while the
		// view camera dollies away; otherwise, the body falls through the ground
		var terrainFocus = new Camera3D { Name = "ShamCam", Current = false };
		AddChild(terrainFocus);

		if (Terrain?.IsClass("Terrain3D") is true)
			Terrain.Call("set_camera", terrainFocus);

		Camera.MakeCurrent();
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
			: Input.GetVector(StrafeLeftAction, StrafeRightAction, MoveForwardAction, MoveBackwardAction);

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
	}
}
