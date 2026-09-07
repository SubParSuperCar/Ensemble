using Godot;
using Root.GdCore.Players;

namespace Root.Scripts.Players;

[GlobalClass]
public partial class PlayerManager : Node
{
	public Godot.Collections.Dictionary<string, PlayerHandle> Handles { get; } = [];

	[Export] public PackedScene PlayerScene { get; set; } = null!;
	[Export] public Node3D TerrainNode { get; set; } = null!;

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
		GetHandleOrNull(playerId) ??
		throw new InvalidOperationException($"Handle with player id {playerId} not found.");

	private void OnPlayerAdded(GdPlayer player)
	{
		var handle = PlayerScene.Instantiate<PlayerHandle>();
		handle.Id = player.Id;
		handle.Name = player.Name;
		handle.TerrainNode = TerrainNode;

		Handles.Add(player.Id, handle);
		AddChild(handle);
	}

	private void OnPlayerRemoved(GdPlayer player)
	{
		if (Handles.Remove(player.Id, out var handle))
			handle.QueueFree();
	}
}
