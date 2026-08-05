using System.Diagnostics;
using Godot;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;
using Serilog;

namespace Root.Core.Gd;

public partial class GdCore : Node
{
	private Impl.Core _core = null!;

	public static GdCore? Instance
	{
		get;
		private set
		{
			field = value;

			Log.Debug("{Class}.{Member} set (hash: {Hash})",
				nameof(GdCore),
				nameof(Instance),
				value?.GetHashCode());
		}
	}

	public GdPlayers Players { get; private set; } = null!;
	public GdAssets Assets { get; private set; } = null!;
	public GdPlots Plots { get; private set; } = null!;

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _Ready()
	{
#if DEBUG
		var stopwatch = Stopwatch.StartNew();
#endif

		_core = new Impl.Core();

		Players = GdPlayers.From(_core.Players);
		Assets = GdAssets.From(_core.Assets);
		Plots = GdPlots.From(_core.Plots);

#if DEBUG
		stopwatch.Stop();

		Log.Debug(
			"{Class} initialized in {Elapsed} ({ElapsedMs:F3} msec)",
			nameof(Impl.Core),
			stopwatch.Elapsed,
			stopwatch.Elapsed.TotalMilliseconds);
#endif
	}

	public void Reset() => _core.Reset();
}
