using Godot;
using Serilog;

namespace Root.Scripts.World;

[GlobalClass]
public partial class WorldHandle : Node3D
{
	public override void _Ready()
	{
#if ENSEMBLE_DEBUG
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

		Log.Debug("{Players}:", nameof(GPlayers));
		foreach (var player in GPlayers.GetAll())
			Log.Debug("{$Player}", player.ToDict());

		Log.Debug("{Assets}:", nameof(GAssets));
		foreach (var asset in GAssets.GetAll())
			Log.Debug("{$Asset}", asset.ToDict());

		Log.Debug("{Plots}:", nameof(GPlots));
		foreach (var plot in GPlots.GetAll())
			Log.Debug("{$Plot}", plot.ToDict());
	}
}
