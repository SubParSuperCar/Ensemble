using Godot;
using Serilog;

namespace Root.Scripts.Main;

public partial class Main : Node
{
	private Node? _gameScene;

	[Export] public PackedScene GameScene { get; set; } = null!;

	public override void _EnterTree()
	{
		GSessionManager.SessionStarted += OnSessionStarted;
		GSessionManager.SessionStopped += OnSessionStopped;
	}

	public override void _ExitTree()
	{
		GSessionManager.SessionStarted -= OnSessionStarted;
		GSessionManager.SessionStopped -= OnSessionStopped;
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressedByEvent("test_session_reset", @event))
			return;

		Log.Information("Restarting session as single-player (test action)...");
		GSessionManager.StartSinglePlayer();
	}

	private void OnSessionStarted()
	{
		Log.Information("Session started (mode: {Mode})", GSessionManager.Mode);

		if (_gameScene is not null)
			return;

		_gameScene = GameScene.Instantiate();
		AddChild(_gameScene);
	}

	private void OnSessionStopped()
	{
		Log.Information("Session stopped");

		GCore.Reset();

		if (_gameScene is null)
			return;

		_gameScene.QueueFree();
		_gameScene = null;
	}
}
