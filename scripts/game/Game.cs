using Godot;
using Serilog;

namespace Root.Scripts.Game;

// TODO: For testing purposes only
[GlobalClass]
public partial class Game : Node3D
{
	public override void _Ready()
	{
#if DEBUG
		{
			const int plotId = 2;

			var plot = GPlots.GetPlot(plotId);
			var instances = plot!.Instances;

			var assets = GAssets.GetAll();
			var count = assets.Count;

			if (count > 0)
			{
				for (var i = 0; i < count; i++)
				{
					const float radius = 5;
					const float y = 1;

					var asset = assets[i];
					var angle = i * Mathf.Tau / count;

					var position = new Vector3(
						Mathf.Cos(angle) * radius,
						y,
						Mathf.Sin(angle) * radius);

					instances.Add(asset.Id, position, Quaternion.Identity);
				}
			}

			GPlayers.Add(string.Empty, "Larpje139 (Test)");
			GPlayers.Add(string.Empty, "Pepsi bottle guzzler (Test)");
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
