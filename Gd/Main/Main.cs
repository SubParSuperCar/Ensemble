using Godot;
using Root.Gd.Globals;

namespace Root.Gd.Main;

public partial class Main : Node
{
	private Node? _gameScene;

	[Export] public PackedScene GameScene { get; set; } = null!;

	public override void _Ready()
	{
		GdGlobals.Host.StartSinglePlayer();

		Console.WriteLine("Players:");

		foreach (var player in Players.GetAll())
			Console.WriteLine(player.ToDict());

		Console.WriteLine("Assets:");

		foreach (var asset in Assets.GetAll())
			Console.WriteLine(asset.ToDict());

		Console.WriteLine("Plots:");

		foreach (var plot in Plots.GetAll())
			Console.WriteLine(plot.ToDict());
	}

	public override void _EnterTree()
	{
		GdGlobals.Host.SessionStarted += OnSessionStarted;
		GdGlobals.Host.SessionStopped += OnSessionStopped;
	}

	public override void _ExitTree()
	{
		GdGlobals.Host.SessionStarted -= OnSessionStarted;
		GdGlobals.Host.SessionStopped -= OnSessionStopped;
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("reset", @event))
			GdGlobals.Host.StartSinglePlayer();
	}

	private void OnSessionStarted()
	{
		Console.WriteLine($"Session started with mode {GdGlobals.Host.Mode}");

		if (_gameScene is not null)
			return;

		_gameScene = GameScene.Instantiate();
		AddChild(_gameScene);
	}

	private void OnSessionStopped()
	{
		Console.WriteLine("Session stopped");

		if (_gameScene is null)
			return;

		_gameScene.QueueFree();
		_gameScene = null;
	}
}
