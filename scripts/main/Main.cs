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

	public override void _Ready() => _ = OnReady();

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("test_session_reset", @event))
			GSessionManager.StartSinglePlayer();
	}

	private static async Task OnReady()
	{
		await Task.Delay((int)(TimeSpan.MillisecondsPerSecond * 1.5)).ConfigureAwait(false);

		Callable.From(() =>
		{
			GSessionManager.StartSinglePlayer();

			Log.Debug(nameof(GPlayers));
			foreach (var player in GPlayers.GetAll())
				Log.Debug("{Player}", player.ToDict().ToString());

			Log.Debug(nameof(GAssets));
			foreach (var asset in GAssets.GetAll())
				Log.Debug("{Asset}", asset.ToDict().ToString());

			Log.Debug(nameof(GPlots));
			foreach (var plot in GPlots.GetAll())
				Log.Debug("{Plot}", plot.ToDict().ToString());
		}).CallDeferred();
	}

	private void OnSessionStarted()
	{
		Log.Information("Session started with mode: {Mode}", GSessionManager.Mode);

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
