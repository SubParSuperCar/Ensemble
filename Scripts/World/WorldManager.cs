using System.Diagnostics;
using EnsembleRoot.Autoloading;
using Godot;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.World;

[GlobalClass]
[Autoload(Order = AutoloadOrder.Late, FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class WorldManager : Node, IAutoload
{
	private const string WorldFailureReason = "The world failed to load.";

	public static WorldManager? Instance { get; private set; }

	public WorldHandle? World { get; private set; }

	[Export] public PackedScene WorldScene { get; set; } = GD.Load<PackedScene>(ScenesDir + "world.tscn");

	public void Initialize()
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

	private void OnSessionStarted()
	{
		Log.Debug("Instantiating and adding {Class}...", nameof(WorldHandle));
		var stopwatch = Stopwatch.StartNew();

		try
		{
			World = WorldScene.Instantiate<WorldHandle>();
			AddChild(World);

			// Godot only logs exceptions thrown in node callbacks, so check that the world's managers registered
			_ = GPlayerManager;
			_ = GAssetManager;
			_ = GPlotManager;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to load {Class}", nameof(WorldHandle));
			GSessionManager.FailSession(WorldFailureReason);

			return;
		}

		stopwatch.Stop();
		Log.Debug(
			"Instantiated and added {Class} in {ElapsedMs:F3} ms",
			nameof(WorldHandle), stopwatch.Elapsed.TotalMilliseconds);
	}

	private void OnSessionStopped()
	{
		Log.Debug("Resetting {Member}...", nameof(GCore));
		var stopwatch = Stopwatch.StartNew();

		GCore.Reset();

		stopwatch.Stop();
		Log.Debug("Reset {Member} in {ElapsedMs:F3} ms", nameof(GCore), stopwatch.Elapsed.TotalMilliseconds);

		if (World is null)
			return;

		RemoveChild(World);
		World.QueueFree();
		World = null;

		Log.Debug("Queued {Class} to be freed", nameof(WorldHandle));
	}
}
