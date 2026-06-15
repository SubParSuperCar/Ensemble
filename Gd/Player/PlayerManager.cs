using Godot;
using Root.Core.Gd.Player;
using Root.Gd.Global;

namespace Root.Gd.Player;

public partial class PlayerManager : Node
{
	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<string, PlayerHandle> Nodes { get; } = [];

	[Export] public PackedScene PlayerScene { get; set; } = null!;

	[Export] public PackedScene CameraScene { get; set; } = null!;

	[Export] public Vector3 SpawnLocation { get; set; }

	public override void _EnterTree()
	{
		GdGlobals.PlayerManager = this;

		foreach (var player in Players.GetAll())
			OnPlayerAdded(player);

		Players.Added += OnPlayerAdded;
		Players.Removed += OnPlayerRemoved;
	}

	private void OnPlayerAdded(GdPlayer player)
	{
		var node = PlayerScene.Instantiate<PlayerHandle>();
		node.Name = player.Name;

		if (player == Players.Local)
		{
			var character = node.GetNode<CharacterBody3D>("Character");
			character.SetScript(node.CharacterControllerScript);

			var camera = CameraScene.Instantiate<Camera.Camera>();
			camera.Focus = character;

			node.AddChild(camera);
		}

		Nodes.Add(player.Id, node);
		AddChild(node);
	}

	private void OnPlayerRemoved(GdPlayer player)
	{
		if (Nodes.Remove(player.Id, out var node))
			node.QueueFree();
	}
}
