using Godot;

namespace Root.Scripts.Game;

// TODO
public partial class Game : Node3D
{
	public override void _Ready()
	{
		const int plotId = 2;
		var plot = GPlots.Get(plotId);
		var instances = plot!.Instances;

		const int assetId = 0;
		var position = Vector3.Down * 15;
		var rotation = Quaternion.Identity;
		instances.Add(assetId, position, rotation);

		GPlayers.Add(string.Empty, "Larpje139");
		GPlayers.Add(string.Empty, "Pepsi bottle violator");
	}
}
