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
	private Game _game = null!;

	public static GdGame Instance { get; private set; } = null!;

	public GdPlayers Players { get; private set; } = null!;
	public GdAssets Assets { get; private set; } = null!;
	public GdPlots Plots { get; private set; } = null!;

	public override void _EnterTree() => Instance = this;

	public override void _Ready()
	{
#if DEBUG
		var stopwatch = Stopwatch.StartNew();
#endif

		_game = new Game();

		Players = GdPlayers.From(_game.Players);
		Assets = GdAssets.From(_game.Assets);
		Plots = GdPlots.From(_game.Plots);

#if DEBUG
		stopwatch.Stop();

		Console.WriteLine(string.Create(
			CultureInfo.InvariantCulture,
			$"GdGame init time: {stopwatch.Elapsed} ({stopwatch.Elapsed.TotalMilliseconds} ms)"));
#endif
	}

	// ReSharper disable once MemberCanBePrivate.Global
	public void Reset() => _game.Reset();
}
