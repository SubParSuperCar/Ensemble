using Godot;
using Root.Core.Gd.Player;
using Root.Gd.Global;

namespace Root.Gd.Player;

// TODO: Add methods or ways to access the nodes of each Player in the back-end
// Since the back-end (GdGlobals.Players) is engine-agonistic/data-based its this classes responsibility to bridge the
// gap for the nodes/actual game objects. Technically just adding/removing the nodes to the below dict is fine
public partial class PlayerManager : Node
{
	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<string, PlayerHandle> Nodes { get; } = [];

	[Export] public PackedScene PlayerScene { get; set; } = null!;

	public override void _EnterTree()
	{
		GdGlobals.PlayerManager = this;

		foreach (var player in Players.GetAll())
			OnPlayerAdded(player);

		Players.Added += OnPlayerAdded;
		Players.Removed += OnPlayerRemoved;
	}

	// Handle removal properly, but don't remove existing back-end player entries
	public override void _ExitTree()
	{
		Players.Added -= OnPlayerAdded;
		Players.Removed -= OnPlayerRemoved;

		if (ReferenceEquals(GdGlobals.PlayerManager, this))
			GdGlobals.PlayerManager = null!;
	}

	private void OnPlayerAdded(GdPlayer player)
	{
		var node = PlayerScene.Instantiate<PlayerHandle>();
		node.Id = player.Id;
		node.Name = player.Name;

		Nodes.Add(player.Id, node);
		AddChild(node);
	}

	private void OnPlayerRemoved(GdPlayer player)
	{
		if (Nodes.Remove(player.Id, out var node))
			node.QueueFree();
	}
}
