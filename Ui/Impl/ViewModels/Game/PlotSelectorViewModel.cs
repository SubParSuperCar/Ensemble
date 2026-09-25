using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels.Utils;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class PlotSelectorViewModel : ViewModelBase
{
	private readonly Dictionary<int, Plot> _plotsById = [];
	private readonly Dictionary<int, Action> _unsubscribeByPlotId = [];

	public PlotSelectorViewModel()
	{
		foreach (var plot in GPlots.GetAll())
			OnPlotAdded(plot);

		GPlots.Added += OnPlotAdded;
		GPlots.Removed += OnPlotRemoved;
	}

	public ObservableCollection<Plot> Plots { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SetPlotToNullCommand))]
	public partial Plot? SelectedPlot { get; set; }

	protected override void OnDispose()
	{
		GPlots.Added -= OnPlotAdded;
		GPlots.Removed -= OnPlotRemoved;

		foreach (var unsubscribe in _unsubscribeByPlotId.Values)
			unsubscribe();
	}

	[RelayCommand(CanExecute = nameof(CanSetPlotToNull))]
	private void SetPlotToNull() => SelectedPlot = null;

	private void OnPlotAdded(GdPlot gdPlot)
	{
		var occupants = gdPlot.Occupants;
		var plot = new Plot { Id = gdPlot.Id };

		UpdateOccupancy();
		occupants.Added += OnOccupantChanged;
		occupants.Removed += OnOccupantChanged;

		OnOwnerChanged(occupants.Owner);
		occupants.OwnerChanged += OnOwnerChanged;

		Plots.Insert(Plots.TakeWhile(other => other.Id < plot.Id).Count(), plot);

		_plotsById.Add(gdPlot.Id, plot);
		_unsubscribeByPlotId.Add(gdPlot.Id, Unsubscribe);

		return;

		void OnOwnerChanged(GdOccupant? owner)
		{
			plot.OwnerName = owner is null ? "<None>" : owner.Player.Name;
		}

		void OnOccupantChanged(GdOccupant _)
		{
			UpdateOccupancy();
		}

		void UpdateOccupancy()
		{
			plot.Occupancy = QuotaFormat.Fraction(occupants.Count, occupants.MaxCount);
		}

		void Unsubscribe()
		{
			occupants.OwnerChanged -= OnOwnerChanged;
			occupants.Added -= OnOccupantChanged;
			occupants.Removed -= OnOccupantChanged;
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

	partial void OnSelectedPlotChanging(Plot? value)
	{
		if (GPlayers.Local is { } local)
			GPlots.SetPlot(local.Id, value?.Id ?? None);
	}

	private bool CanSetPlotToNull() => SelectedPlot is not null;
}

public partial class Plot : ObservableObject
{
	public int Id { get; init; }

	[ObservableProperty] public partial string OwnerName { get; set; } = string.Empty;
	[ObservableProperty] public partial string Occupancy { get; set; } = string.Empty;
}
