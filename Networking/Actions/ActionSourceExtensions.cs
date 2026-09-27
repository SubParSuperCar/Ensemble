using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.SessionManager.Actions;

namespace EnsembleRoot.Networking.Actions;

internal static class ActionSourceExtensions
{
	extension(ActionSource source)
	{
		public GdOccupant? Occupant => GPlots.GetOccupant(source.PlayerId);
		public GdPlot? Plot => source.Occupant?.Plot;
	}
}
