using System.Globalization;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Scripts.Plots.Impl;
using Godot;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Plots;

[GlobalClass]
public partial class PlotManager : Node
{
	// The Godot dictionary is for GDScript and Lua; lookups use the C# one, which doesn't marshal
	private readonly Dictionary<int, PlotHandle> _handlesById = [];

	public Godot.Collections.Dictionary<int, PlotHandle> Handles { get; } = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxOccupantCount { get; set; }

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxTotalInstanceCount { get; set; }

	public override void _EnterTree()
	{
		GPlotManager = this;

		if (GPlots.IsLocked)
		{
			Log.Warning("{Class} is locked", nameof(GPlots));
			return;
		}

		foreach (var handle in GetChildren().OfType<PlotHandle>())
		{
			Handles.Add(handle.Id, handle);
			_handlesById.Add(handle.Id, handle);

			GPlots.Add(
				handle.Id,
				handle.MaxOccupantCount is Default ? DefaultMaxOccupantCount : handle.MaxOccupantCount,
				handle.MaxTotalInstanceCount is Default ? DefaultMaxTotalInstanceCount : handle.MaxTotalInstanceCount);
		}

		GPlots.Lock();
	}

	public override void _Ready()
	{
		UpdateOutlines();

		PlotOutlines.Changed += UpdateOutlines;
		LocalPlotChanged += OnLocalPlotChanged;
	}

	public override void _ExitTree()
	{
		PlotOutlines.Changed -= UpdateOutlines;
		LocalPlotChanged -= OnLocalPlotChanged;

		if (ReferenceEquals(GPlotManager, this))
			GPlotManager = null!;
	}

	public PlotHandle? GetHandleOrNull(int plotId) => _handlesById.GetValueOrDefault(plotId);

	public PlotHandle GetHandle(int plotId) =>
		GetHandleOrNull(plotId) ?? throw new KeyNotFoundException(
			string.Create(CultureInfo.InvariantCulture, $"Handle with plot id {plotId} not found."));

	private void OnLocalPlotChanged(GdPlot? _) => UpdateOutlines();

	private void UpdateOutlines()
	{
		foreach (var (id, handle) in Handles)
			handle.SetOutline(PlotOutlines.GetColor(id));
	}
}
