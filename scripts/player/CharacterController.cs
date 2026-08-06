using Godot;
using Root.Common.Input;

namespace Root.Scripts.Player;

public partial class CharacterController : CharacterBody3D
{
	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m/s")]
	public float WalkSpeed { get; set; } = 6;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m/s")]
	public float RunSpeed { get; set; } = 16;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float JumpHeight { get; set; } = 1.25f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float TurnRate { get; set; } = 11.25f;

	[Export(PropertyHint.Range, "-1,0,or_greater,hide_slider")]
	public float FirstPersonDistance { get; set; } = 1;

	[Export] public Camera3D Camera { get; set; } = null!;

	public override void _Ready() => PhysicsServer3D.BodySetEnableContinuousCollisionDetection(GetRid(), true);

	public override void _PhysicsProcess(double delta)
	{
		Visible = Camera.GlobalPosition.DistanceTo(GlobalPosition) > FirstPersonDistance;

		var gravity = GetGravity();

		if (!IsOnFloor())
			Velocity += gravity * (float)delta;
		else if (!InputExtensions.IsSunk && Input.IsActionPressed("char_jump"))
		{
			var velocity = Velocity;
			velocity.Y = MathF.Sqrt(JumpHeight * 2 * -gravity.Y);

			Velocity = velocity;
		}

		var inputDirection = !InputExtensions.IsSunk
			? Input.GetVector(
				"char_strafe_left", "char_strafe_right",
				"char_move_forward", "char_move_backward")
			: Vector2.Zero;

		if (inputDirection != Vector2.Zero)
		{
			var velocity = Velocity;

			// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
			var lookDirection = Camera?.GlobalBasis ?? Basis.Identity;
			var moveDirection = lookDirection * new Vector3(inputDirection.X, 0, inputDirection.Y);
			moveDirection.Y = 0;

			var speed = Input.IsActionPressed("char_run") ? RunSpeed : WalkSpeed;
			velocity.X = moveDirection.X * speed;
			velocity.Z = moveDirection.Z * speed;

			Velocity = velocity;

			var rotation = Rotation;
			var turnAngle = MathF.Atan2(moveDirection.X, moveDirection.Z);
			rotation.Y = (float)Mathf.LerpAngle(rotation.Y, turnAngle, TurnRate * delta);

			Rotation = rotation;
		}
		else
		{
			var velocity = Velocity;
			velocity.X = 0;
			velocity.Z = 0;

			Velocity = velocity;
		}

		MoveAndSlide();
	}
}
