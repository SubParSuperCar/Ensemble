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
		if (Input.IsActionJustPressedByEvent("reset", @event))
			GSessionManager.StartSinglePlayer();
	}

	private static async Task OnReady()
	{
		await Task.Delay(1500).ConfigureAwait(false);

		Callable.From(() =>
		{
			GSessionManager.StartSinglePlayer();

			Log.Information(nameof(GPlayers));
			foreach (var player in GPlayers.GetAll())
				Log.Information("{Player}", player.ToDict().ToString());

			Log.Information(nameof(GAssets));
			foreach (var asset in GAssets.GetAll())
				Log.Information("{Asset}", asset.ToDict().ToString());

			Log.Information(nameof(GPlots));
			foreach (var plot in GPlots.GetAll())
				Log.Information("{Plot}", plot.ToDict().ToString());
		}).CallDeferred();
	}

	private void OnSessionStarted()
	{
		Log.Information("Session started with mode {Mode}", GSessionManager.Mode);

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
