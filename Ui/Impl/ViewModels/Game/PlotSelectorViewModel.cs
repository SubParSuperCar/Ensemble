using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Sessions.Actions;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels.Utils;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class PlotSelectorViewModel : ViewModelBase, IWidget
{
	private readonly Dictionary<int, PlotItem> _plotsById = [];
	private readonly Dictionary<int, Action> _unsubscribeByPlotId = [];

	public PlotSelectorViewModel()
	{
		foreach (var plot in GPlots.GetAll())
			OnPlotAdded(plot);

		GPlots.Added += OnPlotAdded;
		GPlots.Removed += OnPlotRemoved;

		OnLocalPlotChanged(LocalPlot);
		LocalPlotChanged += OnLocalPlotChanged;

		GSessionManager.ActionRejected += OnActionRejected;
	}

	public ObservableCollection<PlotItem> Plots { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SetPlotToNullCommand))]
	public partial PlotItem? SelectedPlot { get; set; }

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(2, 5, 4, 5))
	{
		Description = "Lets you select which plot you'd like to occupy, if any.",
		MinSize = new Size(288, 160)
	};

	public static void SetHoveredPlot(PlotItem? plot) => PlotOutlines.HoveredPlotId = plot?.Id;

	protected override void OnDispose()
	{
		GPlots.Added -= OnPlotAdded;
		GPlots.Removed -= OnPlotRemoved;

		LocalPlotChanged -= OnLocalPlotChanged;
		GSessionManager.ActionRejected -= OnActionRejected;

		foreach (var unsubscribe in _unsubscribeByPlotId.Values)
			unsubscribe();

		SetHoveredPlot(null);
	}

	partial void OnSelectedPlotChanged(PlotItem? value)
	{
		if (value?.Id != LocalPlot?.Id)
			new SetPlotAction(value?.Id).Submit();
	}

	[RelayCommand(CanExecute = nameof(CanSetPlotToNull))]
	private void SetPlotToNull() => SelectedPlot = null;

	private bool CanSetPlotToNull() => SelectedPlot is not null;

	private void OnPlotAdded(GdPlot gdPlot)
	{
		var occupants = gdPlot.Occupants;
		var plot = new PlotItem { Id = gdPlot.Id };

		UpdateOccupancy();
		occupants.Added += OnOccupantChanged;
		occupants.Removed += OnOccupantChanged;

		OnOwnerChanged(occupants.Owner);
		occupants.OwnerChanged += OnOwnerChanged;

		Plots.Insert(Plots.TakeWhile(other => other.Id < plot.Id).Count(), plot);
		_plotsById.Add(gdPlot.Id, plot);
		_unsubscribeByPlotId.Add(gdPlot.Id, Unsubscribe);

		return;

		void UpdateOccupancy()
		{
			plot.Occupancy = QuotaFormat.Fraction(occupants.Count, occupants.MaxCount);
		}

		void OnOccupantChanged(GdOccupant _)
		{
			UpdateOccupancy();
		}

		void OnOwnerChanged(GdOccupant? owner)
		{
			plot.OwnerName = owner?.Player.Name ?? "<None>";
		}

		void Unsubscribe()
		{
			occupants.Added -= OnOccupantChanged;
			occupants.Removed -= OnOccupantChanged;
			occupants.OwnerChanged -= OnOwnerChanged;
		}
	}

	private void OnPlotRemoved(GdPlot gdPlot)
	{
		if (!_plotsById.Remove(gdPlot.Id, out var plot))
			return;

		Plots.Remove(plot);

		if (_unsubscribeByPlotId.Remove(gdPlot.Id, out var unsubscribe))
			unsubscribe();
	}

	private void OnLocalPlotChanged(GdPlot? plot) =>
		SelectedPlot = plot is null ? null : _plotsById.GetValueOrDefault(plot.Id);

	private void OnActionRejected(string actionId, string reason) => OnLocalPlotChanged(LocalPlot);
}

public sealed partial class PlotItem : ObservableObject
{
	public int Id { get; init; }

	[ObservableProperty] public partial string OwnerName { get; set; } = string.Empty;
	[ObservableProperty] public partial string Occupancy { get; set; } = string.Empty;
}
