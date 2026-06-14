using Godot;
using Root.Globals;

// ReSharper disable MemberCanBePrivate.Global

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

	private async Task StartSinglePlayer()
	{
		await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);

		GdGlobals.Session.StartSinglePlayer();
	}

	public void OnSessionStarted()
	{
		if (_gameScene is not null)
			return;

		_gameScene = GameScene.Instantiate();
		AddChild(_gameScene);
	}

	public void OnSessionStopped()
	{
		if (_gameScene is null)
			return;

		_gameScene.QueueFree();
		_gameScene = null;
	}
}
