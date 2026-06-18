using Godot;
using Root.Gd.Global;

namespace Root.Gd.Plot;

public partial class PlotManager : Node
{
	public Godot.Collections.Dictionary<int, PlotHandle> Nodes { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater")]
	public int DefaultMaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater")]
	public int DefaultMaxTotalInstanceCount { get; set; }

	public override void _EnterTree()
	{
		GdGlobals.PlotManager = this;

		foreach (var handle in GetChildren().OfType<PlotHandle>())
			Plots.Add(handle.Id);
	}
}
