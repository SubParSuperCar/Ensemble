using EnsembleCoreRoot.Api.Players;
using EnsembleCoreRoot.Api.Plots;

namespace EnsembleCoreRoot.Plots;

/// <inheritdoc />
public class Occupant(IPlayer player) : IOccupant
{
	public Plot? Plot { get; private set; }
	public IPlayer Player { get; } = player;

	IPlot? IOccupant.Plot => Plot;

	public event Action<IPlot?>? PlotChanged;

	public override string ToString() => $"Occupant(playerId={Player.Id}, plotId={Plot?.Id})";

	internal void SetPlot(Plot? plot)
	{
		if (ReferenceEquals(plot, Plot))
			return;

		Plot = plot;
		PlotChanged?.Invoke(plot);
	}
}
