using System.Diagnostics;
using System.Globalization;
using Godot;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;

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
			Console.WriteLine($"{nameof(GdCore)}.{nameof(Instance)} set"); // Log for debugging purposes
		}
	}

	public GdPlayers Players { get; private set; } = null!;
	public GdAssets Assets { get; private set; } = null!;
	public GdPlots Plots { get; private set; } = null!;

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		// Only overwrite if it is this instance that is being removed
		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _Ready()
	{
#if DEBUG
		// Log how long it takes to start the whole Core (including the Godot bridge and its main wrappers)
		// Useful for detecting startup performance degradation
		var stopwatch = Stopwatch.StartNew();
#endif

		_core = new Impl.Core();

		Players = GdPlayers.From(_core.Players);
		Assets = GdAssets.From(_core.Assets);
		Plots = GdPlots.From(_core.Plots);

#if DEBUG
		stopwatch.Stop();

		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"{nameof(Impl.Core)} init time: {stopwatch.Elapsed} ({stopwatch.Elapsed.TotalMilliseconds} ms)"));
#endif
	}

	public void Reset() => _core.Reset();
}
