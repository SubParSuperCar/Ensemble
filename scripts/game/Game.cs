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
			var plot = GPlots.Get(plotId);
			var instances = plot!.Instances;

			const int assetId = 0;
			var position = Vector3.Down * 15;
			var rotation = Quaternion.Identity;
			instances.Add(assetId, position, rotation);

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
