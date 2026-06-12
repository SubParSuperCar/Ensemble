using Godot;

namespace Root.Scripts.Plot;

public partial class PlotHandle : Node3D
{
	[Export(PropertyHint.Range, "0,0,1,or_greater")]
	public int Id { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater")]
	public int MaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater")]
	public int MaxTotalInstanceCount { get; set; }
}
