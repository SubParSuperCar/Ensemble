using Godot;

namespace Root.Gd.Player;

public partial class PlayerHandle : Node
{
	[Export] public Script CharacterControllerScript { get; set; } = null!;
}
