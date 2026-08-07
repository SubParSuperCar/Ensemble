using System.Globalization;
using Godot;
using Root.Core.Gd.Plot;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Plot;

[GlobalClass]
public partial class PlotManager : Node
{
	public Godot.Collections.Dictionary<int, PlotHandle> Handles { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxTotalInstanceCount { get; set; }

	public override void _EnterTree()
	{
		GPlotManager = this;

		if (GPlots.IsLocked)
			return;

		foreach (var handle in GetChildren().OfType<PlotHandle>())
		{
			Handles.Add(handle.Id, handle);
			GPlots.Add(handle.Id, handle.MaxOccupantCount, handle.MaxTotalInstanceCount);
		}

		GPlots.Lock();
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(GPlotManager, this))
			GPlotManager = null!;
	}

	public PlotHandle GetHandle(GdPlot plot) => GetHandle(plot.Id);

	public PlotHandle GetHandle(int plotId) =>
		Handles.TryGetValue(plotId, out var handle)
			? handle
			: throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Handle with plot id {plotId} not found"));
}
