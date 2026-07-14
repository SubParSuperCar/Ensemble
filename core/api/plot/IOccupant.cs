using Root.Core.Api.Player;

namespace Root.Core.Api.Plot;

public interface IOccupant
{
	IPlayer Player { get; }

	IPlot? Plot { get; }
	event Action<IPlot?> PlotChanged;
}
