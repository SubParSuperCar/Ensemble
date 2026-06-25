using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Core.Gd.Plot;

namespace Root.Ui.Impl.ViewModels;

public partial class PlotSelectorViewModel : ViewModelBase
{
	private readonly Dictionary<int, Plot> _plotsById = [];

	public PlotSelectorViewModel()
	{
		foreach (var plot in GPlots.GetAll())
			OnPlotAdded(plot);

		GPlots.Added += OnPlotAdded;
	}

	public ObservableCollection<Plot> Plots { get; } = [];
	[ObservableProperty] public partial Plot? SelectedItem { get; set; } = null!;

	public override void Dispose()
	{
		GPlots.Added -= OnPlotAdded;

		foreach (var plot in GPlots.GetAll())
			OnPlotRemoved(plot);

		GC.SuppressFinalize(this);
	}

	[RelayCommand]
	private void SetPlotToNull() => SelectedItem = null!;

	private void OnPlotAdded(GdPlot gdPlot)
	{
		var occupants = gdPlot.Occupants;

		Plot plot = null!;
		plot = new Plot
		{
			OnRemoved = OnRemoved,
			Id = gdPlot.Id
		};

		// TODO: Fix bug where event references "slide out from under" when GCore.Reset is called
		OnOccupantAddedOrRemoved(null!);
		occupants.Added += OnOccupantAddedOrRemoved;
		occupants.Removed += OnOccupantAddedOrRemoved;

		Plots.Add(plot);
		_plotsById[gdPlot.Id] = plot;

		return;

		void OnOccupantAddedOrRemoved(GdOccupant occupant)
		{
			// ReSharper disable once AccessToModifiedClosure
			plot.Occupancy = string.Create(CultureInfo.InvariantCulture,
				$"{occupants.Count} / {occupants.MaxCount}");
		}

		void OnRemoved()
		{
			occupants.Removed -= OnOccupantAddedOrRemoved;
			occupants.Removed -= OnOccupantAddedOrRemoved;
		}
	}

	private void OnPlotRemoved(GdPlot gdPlot)
	{
		if (!_plotsById.Remove(gdPlot.Id, out var plot))
			return;

		Plots.Remove(plot);
		plot.OnRemoved();
	}

	partial void OnSelectedItemChanging(Plot? value) => GPlots.SetPlot(GPlayers.Local!.Id, value?.Id ?? -1);
}

public partial class Plot : ObservableObject
{
	public required Action OnRemoved { get; init; }

	public int Id { get; init; }

	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Occupancy { get; set; } = string.Empty;
}
