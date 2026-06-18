using Godot;
using Root.Gd.Globals;

namespace Root.Gd.Main;

public partial class Main : Node
{
	private Node? _gameScene;

	[Export] public PackedScene GameScene { get; set; } = null!;

	public override void _Ready() => _ = StartSinglePlayer();

	public override void _EnterTree()
	{
		GdGlobals.Session.SessionStarted += OnSessionStarted;
		GdGlobals.Session.SessionStopped += OnSessionStopped;
	}

	public override void _ExitTree()
	{
		GdGlobals.Session.SessionStarted -= OnSessionStarted;
		GdGlobals.Session.SessionStopped -= OnSessionStopped;
	}

	private void OnSessionStarted()
	{
		if (_gameScene is not null)
			return;

		_gameScene = GameScene.Instantiate();
		AddChild(_gameScene);
	}

	private void OnSessionStopped()
	{
		if (_gameScene is null)
			return;

		_gameScene.QueueFree();
		_gameScene = null;
	}

	private async Task StartSinglePlayer()
	{
		await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);

		GdGlobals.Session.StartSinglePlayer();

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
}
