using Godot;
using Root.Autoloading;
using Serilog;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Root.Scripts.World;

[GlobalClass]
[Autoload(Order = sbyte.MaxValue - 1, FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class WorldManager : Node, IAutoload
{
	public WorldHandle? World { get; private set; }

	public static WorldManager? Instance { get; private set; }

	[Export] public PackedScene WorldScene { get; set; } = GD.Load<PackedScene>(ScenesDir + "/world.tscn");

	public void Initialize()
	{
		Instance = this;

		GSessionManager.SessionStarted += OnSessionStarted;
		GSessionManager.SessionStopped += OnSessionStopped;

		GSessionManager.StartSinglePlayer();
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

		World = WorldScene.Instantiate() as WorldHandle;
		AddChild(World);

		stopwatch.Stop();
		Log.Debug(
			"Instantiated and added {Class} in {ElapsedMs:F3} ms.",
			nameof(WorldHandle),
			stopwatch.Elapsed.TotalMilliseconds);
	}

	private void OnSessionStopped()
	{
		Log.Debug("Resetting {Member}...", nameof(GCore));
		var stopwatch = Stopwatch.StartNew();

		GCore.Reset();

		stopwatch.Stop();
		Log.Debug("Reset {Member} in {ElapsedMs:F3} ms.", nameof(GCore), stopwatch.Elapsed.TotalMilliseconds);

		if (World is null)
			return;

		World.QueueFree();
		World = null;

		Log.Debug("Queued {Class} to be freed.", nameof(WorldHandle));
	}
}
