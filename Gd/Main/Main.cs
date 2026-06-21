using Godot;
using Root.Gd.Globals;

namespace Root.Gd.Main;

public partial class Main : Node
{
	private Node? _gameScene;

	[Export] public PackedScene GameScene { get; set; } = null!;

	public override void _Ready()
	{
		_ = ResetSinglePlayerSessionAsync();

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
		if (Input.IsActionJustPressed("reset"))
			_ = ResetSinglePlayerSessionAsync();
	}

	private async Task ResetSinglePlayerSessionAsync()
	{
		Console.WriteLine("Resetting single player session...");

		GdGlobals.Host.StopSession();

		await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);

		GdGlobals.Host.StartSinglePlayer();

		Console.WriteLine("Single player session reset");
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
