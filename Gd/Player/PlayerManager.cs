using Godot;
using Root.Core.Gd.Player;
using Root.Gd.Globals;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Gd.Player;

public partial class PlayerManager : Node
{
	public Godot.Collections.Dictionary<string, PlayerHandle> Handles { get; } = [];

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

	public PlayerHandle GetHandle(string playerId)
		=> Handles.TryGetValue(playerId, out var handle)
			? handle
			: throw new InvalidOperationException($"Handle with player id {playerId} not found");

	private void OnPlayerAdded(GdPlayer player)
	{
		var node = PlayerScene.Instantiate<PlayerHandle>();
		node.Id = player.Id;
		node.Name = player.Name;

		Handles.Add(player.Id, node);
		AddChild(node);
	}

	private void OnPlayerRemoved(GdPlayer player)
	{
		if (Handles.Remove(player.Id, out var node))
			node.QueueFree();
	}
}
