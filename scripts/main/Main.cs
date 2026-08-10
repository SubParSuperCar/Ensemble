using Godot;
using Serilog;

namespace Root.Scripts.Main;

[GlobalClass]
public partial class Main : Node
{
	private Node? _game;

	public static Node? Game { get; private set; }

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

		if (_game is not null)
			return;

		_game = GameScene.Instantiate();
		AddChild(_game);

		Game = _game;
	}

	private void OnSessionStopped()
	{
		Log.Information("Session stopped");

		GCore.Reset();

		if (_game is null)
			return;

		_game.QueueFree();
		_game = null;
	}
}
