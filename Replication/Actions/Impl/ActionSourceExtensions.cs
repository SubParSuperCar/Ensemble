using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Sessions.Actions;

namespace EnsembleRoot.Replication.Actions;

internal static class ActionSourceExtensions
{
	extension(ActionSource source)
	{
		public GdOccupant? Occupant => GPlots.GetOccupant(source.PlayerId);
		public GdPlot? Plot => source.Occupant?.Plot;
	}
}
