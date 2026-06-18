using Root.Core.Api.Player;
using Root.Core.Api.Plot;

namespace Root.Core.Impl.Plot;

public class Occupant(IPlayer player) : IOccupant
{
	public IPlayer Player { get; } = player;
	public Plot? Plot { get; private set; }

	IPlot? IOccupant.Plot => Plot;

	public event Action<IPlot?>? PlotChanged;

	public void SetPlot(Plot? plot)
	{
		if (ReferenceEquals(plot, Plot))
			return;

		Plot = plot;
		PlotChanged?.Invoke(plot);
	}

	public override string ToString() => $"Occupant(playerId={Player.Id}, plotId={Plot?.Id})";
}
