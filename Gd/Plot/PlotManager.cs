using System.Globalization;
using Godot;
using Root.Core.Gd.Plot;
using Root.Gd.Globals;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Gd.Plot;

public partial class PlotManager : Node
{
	public Godot.Collections.Dictionary<int, PlotHandle> Handles { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxTotalInstanceCount { get; set; }

	public override void _Ready()
	{
		GdGlobals.PlotManager = this;

		if (Plots.IsLocked)
			return;

		foreach (var handle in GetChildren().OfType<PlotHandle>())
			Plots.Add(handle.Id, handle.MaxOccupantCount, handle.MaxTotalInstanceCount);

		Plots.Lock();
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(GdGlobals.PlotManager, this))
			GdGlobals.PlotManager = null!;
	}

	public PlotHandle GetHandle(GdPlot plot) => GetHandle(plot.Id);

	public PlotHandle GetHandle(int plotId)
		=> Handles.TryGetValue(plotId, out var handle)
			? handle
			: throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Handle with plot id {plotId} not found"));
}
