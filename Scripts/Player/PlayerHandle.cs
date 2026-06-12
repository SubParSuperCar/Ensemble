using Godot;

namespace Root.Scripts.Player;

public partial class PlayerHandle : Node3D
{
	[Export] public Script CharacterControllerScript { get; set; } = null!;
}
