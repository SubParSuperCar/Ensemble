using CoreRoot.Api.Players;

namespace CoreRoot.Api.Plots;

/// <summary>
///     The representation of an <see cref="IPlayer" /> object's occupancy on an <see cref="IPlot" />, if any.
/// </summary>
public interface IOccupant
{
	IPlayer Player { get; }

	IPlot? Plot { get; }
	event Action<IPlot?> PlotChanged;
}
