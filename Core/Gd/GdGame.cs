using System.Diagnostics;
using System.Globalization;
using Godot;
using Root.Core.Gd.Asset;
using Root.Core.Gd.Player;
using Root.Core.Gd.Plot;
using Root.Core.Impl;

namespace Root.Core.Gd;

public partial class GdGame : Node
{
	public static GdGame? Instance { get; private set; }

	public GdPlayers Players { get; private set; } = null!;
	public GdAssets Assets { get; private set; } = null!;
	public GdPlots Plots { get; private set; } = null!;

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _Ready() => Reset();

	private void Reset()
	{
#if DEBUG
		var stopwatch = Stopwatch.StartNew();
#endif

		var game = new Game();

		Players = GdPlayers.From(game.Players);
		Assets = GdAssets.From(game.Assets);
		Plots = GdPlots.From(game.Plots);

#if DEBUG
		stopwatch.Stop();

		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"GdGame init time: {stopwatch.Elapsed} ({stopwatch.Elapsed.TotalMilliseconds} ms)"));
#endif
	}
}
