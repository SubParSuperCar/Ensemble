using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Core.Gd.Plot;

// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable AccessToModifiedClosure

namespace Root.Ui.Impl.ViewModels;

public partial class PlotSelectorViewModel : ViewModelBase
{
	private readonly Dictionary<int, Plot> _plotsById = [];

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
	public partial Plot? SelectedItem { get; set; }

	public override void Dispose()
	{
		GPlots.Added -= OnPlotAdded;
		GPlots.Removed -= OnPlotRemoved;

		foreach (var plot in GPlots.GetAll())
			OnPlotRemoved(plot);

		GC.SuppressFinalize(this);
	}

	[RelayCommand(CanExecute = nameof(CanSetPlotToNull))]
	private void SetPlotToNull() => SelectedItem = null;

#pragma warning disable MA0051
	private void OnPlotAdded(GdPlot gdPlot)
#pragma warning restore MA0051
	{
		var occupants = gdPlot.Occupants;

		Plot plot = null!;
		plot = new Plot
		{
			OnRemoved = OnRemoved,
			Id = gdPlot.Id
		};

		OnOwnerChanged(occupants.Owner);
		occupants.OwnerChanged += OnOwnerChanged;

		OnOccupantAddedOrRemoved(null!);
		occupants.Added += OnOccupantAddedOrRemoved;
		occupants.Removed += OnOccupantAddedOrRemoved;

		Plots.Add(plot);
		_plotsById[gdPlot.Id] = plot;

		return;

		void OnOwnerChanged(GdOccupant? owner)
		{
			if (owner is null)
			{
				plot.OwnerName = "<Null>";
				return;
			}

			const char delimiter = '-';

			var span = owner.Player.Name.AsSpan();
			var wasTruncated = false;

			var first = span.IndexOf(delimiter);
			if (first != -1)
			{
				var second = span[++first..].IndexOf(delimiter);
				if (second != -1)
				{
					span = span[..(first + second)];
					wasTruncated = true;
				}
			}

			plot.OwnerName = wasTruncated ? $"{span}..." : span.ToString();
		}

		void OnOccupantAddedOrRemoved(GdOccupant occupant)
		{
			plot.Occupancy = string.Create(CultureInfo.InvariantCulture,
				$"{occupants.Count} / {(occupants.MaxCount == -1 ? "<Unlimited>" : occupants.MaxCount)}");
		}

		void OnRemoved()
		{
			occupants.OwnerChanged -= OnOwnerChanged;

			occupants.Added -= OnOccupantAddedOrRemoved;
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

	private bool CanSetPlotToNull() => SelectedItem is not null;
}

public partial class Plot : ObservableObject
{
	public required Action OnRemoved { get; init; }

	public int Id { get; init; }

	[ObservableProperty] public partial string? OwnerName { get; set; }
	[ObservableProperty] public partial string Occupancy { get; set; } = string.Empty;
}
