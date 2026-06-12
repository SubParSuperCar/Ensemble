using Godot;

namespace Root.Scripts.Player;

public partial class CharacterController : CharacterBody3D
{
	[Export(PropertyHint.Range, "0,0,or_greater,suffix:m/s")]
	public float WalkSpeed { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater,suffix:m/s")]
	public float RunSpeed { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater,suffix:m")]
	public float JumpHeight { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float TurnRate { get; set; }

	[Export] public Node Camera { get; set; } = null!;
}
