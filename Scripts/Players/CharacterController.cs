using EnsembleRoot.Common.Input;
using Godot;

// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract

namespace EnsembleRoot.Scripts.Players;

[GlobalClass]
public partial class CharacterController : CharacterBody3D
{
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
	[Export] public Node3D Terrain { get; set; } = null!;

	public override void _Ready()
	{
		PhysicsServer3D.BodySetEnableContinuousCollisionDetection(GetRid(), true);

		var terrainFocus = new Camera3D { Name = "ShamCam", Current = false };
		AddChild(terrainFocus);

		if (Terrain?.IsClass("Terrain3D") is true)
			Terrain.Call("set_camera", terrainFocus);

		Camera?.MakeCurrent();
	}

	public override void _PhysicsProcess(double delta)
	{
		Visible = Camera.GlobalPosition.DistanceTo(GlobalPosition) > FirstPersonInvisibleProximityThreshold;

		var velocity = Velocity;

		if (!IsOnFloor())
			velocity += GetGravity() * (float)delta;
		else if (!InputSink.IsSunk && Input.IsActionPressed("char_jump"))
			velocity.Y = MathF.Sqrt(JumpHeight * 2 * -GetGravity().Y);

		var inputDirection = !InputSink.IsSunk
			? Input.GetVector(
				"char_strafe_left", "char_strafe_right",
				"char_move_forward", "char_move_backward")
			: Vector2.Zero;

		if (inputDirection != Vector2.Zero)
		{
			var cameraYaw = Camera?.GlobalRotation.Y ?? 0;
			var moveDirection = new Vector3(inputDirection.X, 0, inputDirection.Y).Rotated(Vector3.Up, cameraYaw);

			var speed = Input.IsActionPressed("char_run") ? RunSpeed : WalkSpeed;
			velocity.X = moveDirection.X * speed;
			velocity.Z = moveDirection.Z * speed;

			var rotation = Rotation;
			var turnAngle = MathF.Atan2(moveDirection.X, moveDirection.Z);
			rotation.Y = (float)Mathf.LerpAngle(rotation.Y, turnAngle, TurnRate * delta);

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
