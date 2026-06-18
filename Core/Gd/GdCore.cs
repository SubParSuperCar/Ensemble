using System.Diagnostics;
using System.Globalization;
using Godot;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;

namespace Root.Core.Gd;

public partial class GdCore : Node
{
	public static GdCore? Instance
	{
		get;
		private set
		{
			field = value;
			Console.WriteLine($"{nameof(GdCore)}.{nameof(Instance)} has been set");
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

		var core = new Impl.Core();

		Players = GdPlayers.From(core.Players);
		Assets = GdAssets.From(core.Assets);
		Plots = GdPlots.From(core.Plots);

#if DEBUG
		stopwatch.Stop();

		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"{nameof(Impl.Core)} init time: {stopwatch.Elapsed} ({stopwatch.Elapsed.TotalMilliseconds} ms)"));
#endif
	}
}
