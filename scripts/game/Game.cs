using Godot;

namespace Root.Scripts.Test;

// TODO: For testing purposes only
public partial class Game : Node3D
{
	public override void _Ready()
	{
		const int plotId = 2; // Plot ID referencing the 3rd plot (IDs start from 0)
		var plot = GPlots.Get(plotId); // Get the 'Plot' object
		var instances = plot!.Instances; // Get the 'Instances' object

		const int assetId = 0; // Asset ID referencing a simple cube/block
		var position = Vector3.Down * 15; // Down 15 units from the center of the Plot
		var rotation = Quaternion.Identity; // Default rotation
		instances.Add(assetId, position, rotation); // Add the 'Instance' object

		GPlayers.Add(string.Empty, "Larpje139");
		GPlayers.Add(string.Empty, "Pepsi bottle violator");
	}
}
