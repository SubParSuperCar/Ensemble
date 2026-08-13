using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Core.Gd.Plot;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

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

		var index = Plots
			.TakeWhile(p => string.Compare(
				p.Id.ToString(CultureInfo.InvariantCulture),
				plot.Id.ToString(CultureInfo.InvariantCulture),
				StringComparison.Ordinal) < 0)
			.Count();

		Plots.Insert(index, plot);

		_plotsById[gdPlot.Id] = plot;
		_unsubscribeByPlotId[gdPlot.Id] = Unsubscribe;

		return;

		void OnOwnerChanged(GdOccupant? owner)
		{
			plot.OwnerName = owner is null ? "<Null>" : owner.Player.Name;
		}

		void OnOccupantChanged(GdOccupant occupant)
		{
			UpdateOccupancy();
		}

		void UpdateOccupancy()
		{
			plot.Occupancy = string.Create(CultureInfo.InvariantCulture,
				$"{occupants.Count} / {(occupants.MaxCount is Unlimited ? "<Unlimited>" : occupants.MaxCount)}");
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
