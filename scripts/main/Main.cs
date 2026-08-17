using System.Diagnostics;
using Godot;
using Serilog;

namespace Root.Scripts.Main;

[GlobalClass]
public partial class Main : Node
{
	public Game.Game? Game { get; private set; }

	public static Main? Instance { get; private set; }

	[Export] public PackedScene GameScene { get; set; } = null!;

	public override void _EnterTree()
	{
		Instance = this;

		GSessionManager.SessionStarted += OnSessionStarted;
		GSessionManager.SessionStopped += OnSessionStopped;
	}

	public override void _ExitTree()
	{
		GSessionManager.SessionStarted -= OnSessionStarted;
		GSessionManager.SessionStopped -= OnSessionStopped;

		if (ReferenceEquals(Instance, this))
			Instance = null;
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

		if (Game is not null)
			return;

		Log.Debug("Instantiating and adding {Node}", nameof(Scripts.Game.Game));
		var stopwatch = Stopwatch.StartNew();

		Game = GameScene.Instantiate() as Game.Game;
		AddChild(Game);

		stopwatch.Stop();
		Log.Debug(
			"{Node} instantiated and added in {Elapsed} ({ElapsedMs:F3} msec)",
			nameof(Scripts.Game.Game),
			stopwatch.Elapsed,
			stopwatch.Elapsed.TotalMilliseconds);
	}

	private void OnSessionStopped()
	{
		Log.Information("Session stopped");

		GCore.Reset();

		if (Game is null)
			return;

		Game.QueueFree();
		Game = null;
	}
}
