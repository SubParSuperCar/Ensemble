using Godot;
using Root.Gd.Globals;

namespace Root.Gd.Plot;

public partial class PlotManager : Node
{
	// ReSharper disable once MemberCanBePrivate.Global
	public Godot.Collections.Dictionary<int, PlotHandle> Nodes { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxTotalInstanceCount { get; set; }

	public override void _Ready()
	{
		GdGlobals.PlotManager = this;

		foreach (var handle in GetChildren().OfType<PlotHandle>())
			Plots.Add(handle.Id, handle.MaxOccupantCount, handle.MaxTotalInstanceCount);

		Plots.Lock();
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(GdGlobals.PlotManager, this))
			GdGlobals.PlotManager = null!;
	}
}
