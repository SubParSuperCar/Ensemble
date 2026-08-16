using System.Diagnostics;
using Godot;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;
using Serilog;

namespace Root.Core.Gd;

public partial class GdCore : Node
{
	// ReSharper disable once MemberCanBePrivate.Global
	public Impl.Core Core { get; private set; } = null!;

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
#if ENSEMBLE_DEBUG
		var stopwatch = Stopwatch.StartNew();
#endif

		Core = new Impl.Core();

		Players = GdPlayers.From(Core.Players);
		Assets = GdAssets.From(Core.Assets);
		Plots = GdPlots.From(Core.Plots);

#if ENSEMBLE_DEBUG
		stopwatch.Stop();

		Log.Debug(
			"{Class} initialized in {Elapsed} ({ElapsedMs:F3} msec)",
			nameof(Impl.Core),
			stopwatch.Elapsed,
			stopwatch.Elapsed.TotalMilliseconds);
#endif
	}

	public void Reset() => Core.Reset();
}
