using Godot;

namespace Root.Gd.Player;

public partial class CharacterController : CharacterBody3D
{
	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m/s")]
	public float WalkSpeed { get; set; } = 5;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m/s")]
	public float RunSpeed { get; set; } = 10;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float JumpHeight { get; set; } = 1;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float TurnRate { get; set; } = 10;

	[Export] public Node3D Camera { get; set; } = null!; // Reference for which way the player is looking. Can be null

	public override void _Ready() => // Enable the better predictive collisions for UX/less clipping through objects
		PhysicsServer3D.BodySetEnableContinuousCollisionDetection(GetRid(), true);

	public override void _PhysicsProcess(double delta)
	{
		var gravity = GetGravity(); // Gravity can always change

		if (!IsOnFloor()) // In the air
			Velocity += gravity * (float)delta;
		else if (Input.IsActionPressed("jump")) // On the ground requesting to jump. Keeps jumping as long as pressed
		{
			var velocity = Velocity;
			velocity.Y = MathF.Sqrt(JumpHeight * 2f * -gravity.Y); // Hopefully this math is correct

			Velocity = velocity;
		}

		var inputDirection = Input.GetVector(
			"strafe_left", "strafe_right",
			"move_forward", "move_backward");

		if (inputDirection != Vector2.Zero)
		{
			var velocity = Velocity; // Cache for performance and also cannot directly modify components (X/Y/Z)

			// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
			var lookDirection = Camera?.GlobalBasis ?? Basis.Identity; // Dir cam is looking at. Can fall back
			var moveDirection = lookDirection * new Vector3(inputDirection.X, 0, inputDirection.Y);
			moveDirection.Y = 0;

			var moveVelocity = Input.IsActionPressed("run") ? RunSpeed : WalkSpeed;
			velocity.X = moveDirection.X * moveVelocity;
			velocity.Z = moveDirection.Z * moveVelocity;

			Velocity = velocity;

			var rotation = Rotation;
			var turnAngle = MathF.Atan2(moveDirection.X, moveDirection.Z); // The target angle to turn toward
			rotation.Y = (float)Mathf.LerpAngle(rotation.Y, turnAngle, TurnRate * delta);

			Rotation = rotation;
		}
		else
		{
			// No lerped slowing, just instantly stop
			// Physics-based slowing with friction calculations can come at a later date but not now
			var velocity = Velocity;
			velocity.X = 0;
			velocity.Z = 0;

			Velocity = velocity;
		}

		MoveAndSlide();
	}
}
