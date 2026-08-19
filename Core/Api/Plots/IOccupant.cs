using CoreRoot.Api.Players;

namespace CoreRoot.Api.Plots;

public interface IOccupant
{
	IPlayer Player { get; }

	IPlot? Plot { get; }
	event Action<IPlot?> PlotChanged;
}
