using Godot;
using Serilog;
#if ENSEMBLE_DEBUG
using EnsembleRoot.SessionManager.Api;
#endif

namespace EnsembleRoot.Scripts.World;

[GlobalClass]
public partial class WorldHandle : Node3D
{
	public override void _Ready()
	{
#if ENSEMBLE_DEBUG
		if (GSessionManager.Mode is SessionMode.SinglePlayer)
		{
			const int plotId = 2;
			const float y = 0.5f;

			var instances = GPlots.GetPlot(plotId)!.Instances;

			var assets = GAssets.GetAll();
			var count = assets.Count;

			for (var i = 0; i < count; i++)
			{
				var asset = assets[i];
				var angle = i * Mathf.Tau / count;

				var position = new Vector3(Mathf.Cos(angle) * count, y, Mathf.Sin(angle) * count);
				position = (position - Vector3.One * 0.5f).Round() + Vector3.One * 0.5f;

				instances.Add(asset.Id, position, Quaternion.Identity);
			}

			GPlayers.Add(string.Empty, "Foo - Larpje139 (Test)");
			GPlayers.Add(string.Empty, "Bar - Diet Dr. Thunder Enjoyer (Test)");
			GPlayers.Add(string.Empty, "Baz - Dr. Jr. (Test)");
		}
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
}
