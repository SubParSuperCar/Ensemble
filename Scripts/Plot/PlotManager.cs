using Godot;
using Root.Globals;

namespace Root.Scripts.Plot;

public partial class PlotManager : Node
{
	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<int, PlotHandle> Nodes { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater")]
	public int DefaultMaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater")]
	public int DefaultMaxTotalInstanceCount { get; set; }

	public override void _EnterTree() => GdGlobals.PlotManager = this;
}
