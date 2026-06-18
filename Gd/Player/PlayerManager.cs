using Godot;
using Root.Core.Gd.Player;
using Root.Gd.Global;

namespace Root.Gd.Player;

public partial class PlayerManager : Node
{
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
