using Root.Core.Api.Player;
using Root.Core.Api.Plot;

namespace Root.Core.Impl.Plot;

public class Occupant(IPlayer player) : IOccupant
{
	public Plot? Plot { get; private set; }
	public IPlayer Player { get; } = player;

	// Expose both the interface and concrete classes for internal purposes
	IPlot? IOccupant.Plot => Plot;

	public event Action<IPlot?>? PlotChanged;

	public void SetPlot(Plot? plot)
	{
		// If the provided Plot is the same as the current one, drop it
		if (ReferenceEquals(plot, Plot))
			return;

		Plot = plot;
		PlotChanged?.Invoke(plot);
	}

	public override string ToString() => $"Occupant(playerId={Player.Id}, plotId={Plot?.Id})";
}
