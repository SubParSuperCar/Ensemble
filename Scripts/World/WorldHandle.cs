using Godot;
using Serilog;
#if ENSEMBLE_DEBUG
using EnsembleRoot.Sessions.Api;
#endif

namespace EnsembleRoot.Scripts.World;

[GlobalClass]
public partial class WorldHandle : Node3D
{
	public override void _Ready()
	{
#if ENSEMBLE_DEBUG
		if (GSessionManager.Mode is SessionMode.SinglePlayer)
			AddTestData();
#endif

		Log.Debug("{Member} ({Count}):", nameof(GPlayers), GPlayers.Count);
		foreach (var player in GPlayers.GetAll().OrderBy(static player => player.Id, StringComparer.Ordinal))
			Log.Debug("{$Player}", player.ToDict());

		Log.Debug("{Member} ({Count}):", nameof(GAssets), GAssets.Count);
		foreach (var asset in GAssets.GetAll().OrderBy(static asset => asset.Id))
			Log.Debug("{$Asset}", asset.ToDict());

		Log.Debug("{Member} ({Count}):", nameof(GPlots), GPlots.Count);
		foreach (var plot in GPlots.GetAll().OrderBy(static plot => plot.Id))
		{
			var dict = plot.ToDict();
			dict.Add("occupants", plot.Occupants.GetAll().Select(static occupant => occupant.Player.Id).ToArray());
			dict.Add("maxOccupantCount", plot.Occupants.MaxCount);
			dict.Add("maxTotalInstanceCount", plot.Instances.MaxCount);

			Log.Debug("{$Plot}", dict);
		}
	}

#if ENSEMBLE_DEBUG
	// Places every asset in a ring on a test plot (radius = asset count, snapped to cell centers) and adds fake players
	private static void AddTestData()
	{
		var instances = GPlots.GetPlot(TestPlotId)!.Instances;
		var assets = GAssets.GetAll();
		var count = assets.Count;

		for (var i = 0; i < count; i++)
		{
			var angle = i * float.Tau / count;
			var position = new Vector3(MathF.Cos(angle) * count, 0.5f, MathF.Sin(angle) * count);

			instances.Add(assets[i].Id, (position - HalfCell).Round() + HalfCell, Quaternion.Identity);
		}

		GPlayers.Add(string.Empty, "Foo - Larpje139 (Test)");
		GPlayers.Add(string.Empty, "Bar - Diet Dr. Thunder Enjoyer (Test)");
		GPlayers.Add(string.Empty, "Baz - Dr. Jr. (Test)");
	}
#endif
#if ENSEMBLE_DEBUG
	private const int TestPlotId = 2;

	private static readonly Vector3 HalfCell = Vector3.One * 0.5f;
#endif
}
