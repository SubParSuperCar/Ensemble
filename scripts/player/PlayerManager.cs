using Godot;
using Root.Core.Gd.Player;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Player;

[GlobalClass]
public partial class PlayerManager : Node
{
	public Godot.Collections.Dictionary<string, PlayerHandle> Handles { get; } = [];

	[Export] public PackedScene PlayerScene { get; set; } = null!;

	public override void _EnterTree() => GPlayerManager = this;

	public override void _ExitTree()
	{
		GPlayers.Added -= OnPlayerAdded;
		GPlayers.Removed -= OnPlayerRemoved;

		if (ReferenceEquals(GPlayerManager, this))
			GPlayerManager = null!;
	}

	public override void _Ready()
	{
		foreach (var player in GPlayers.GetAll())
			OnPlayerAdded(player);

		GPlayers.Added += OnPlayerAdded;
		GPlayers.Removed += OnPlayerRemoved;
	}

	public PlayerHandle? GetHandleOrNull(string playerId) =>
		Handles.TryGetValue(playerId, out var handle) ? handle : null;

	public PlayerHandle GetHandle(string playerId) =>
		GetHandleOrNull(playerId) ?? throw new InvalidOperationException($"Handle with player id {playerId} not found");

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
