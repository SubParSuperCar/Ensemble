using Godot;
using Root.Globals;

namespace Root.Scripts.Player;

public partial class PlayerManager : Node
{
	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<string, PlayerHandle> Nodes { get; } = [];

	[Export] public PackedScene PlayerScene { get; set; } = null!;

	[Export] public PackedScene CameraScene { get; set; } = null!;

	[Export] public Vector3 SpawnLocation { get; set; }

	public override void _EnterTree() => GdGlobals.PlayerManager = this;
}
