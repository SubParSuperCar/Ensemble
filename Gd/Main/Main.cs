using Godot;

namespace Root.Gd.Main;

public partial class Main : Node
{
	private Node? _gameScene;

	[Export] public PackedScene GameScene { get; set; } = null!;

	public override void _Ready()
	{
		GHost.StartSinglePlayer();

		Console.WriteLine("Players:");

		foreach (var player in GPlayers.GetAll())
			Console.WriteLine(player.ToDict());

		Console.WriteLine("Assets:");

		foreach (var asset in GAssets.GetAll())
			Console.WriteLine(asset.ToDict());

		Console.WriteLine("Plots:");

		foreach (var plot in GPlots.GetAll())
			Console.WriteLine(plot.ToDict());
	}

	public override void _EnterTree()
	{
		GHost.SessionStarted += OnSessionStarted;
		GHost.SessionStopped += OnSessionStopped;
	}

	public override void _ExitTree()
	{
		GHost.SessionStarted -= OnSessionStarted;
		GHost.SessionStopped -= OnSessionStopped;
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("reset", @event))
			GHost.StartSinglePlayer();
	}

	private void OnSessionStarted()
	{
		Console.WriteLine($"Session started with mode {GHost.Mode}");

		if (_gameScene is not null)
			return;

		_gameScene = GameScene.Instantiate();
		AddChild(_gameScene);
	}

	private void OnSessionStopped()
	{
		Console.WriteLine("Session stopped");

		GCore.Reset();

		if (_gameScene is null)
			return;

		_gameScene.QueueFree();
		_gameScene = null;
	}
}
