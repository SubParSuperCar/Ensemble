using CoreRoot.Api.Players;
using CoreRoot.Api.Plots;

namespace CoreRoot.Impl.Plots;

public class Occupant(IPlayer player) : IOccupant
{
	public Plot? Plot { get; private set; }
	public IPlayer Player { get; } = player;

	IPlot? IOccupant.Plot => Plot;

	public event Action<IPlot?>? PlotChanged;

	internal void SetPlot(Plot? plot)
	{
		if (ReferenceEquals(plot, Plot))
			return;

		Plot = plot;
		PlotChanged?.Invoke(plot);
	}

	public override string ToString() => $"Occupant(playerId={Player.Id}, plotId={Plot?.Id})";
}
