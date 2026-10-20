// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	// The host alone arbitrates, from the creations it has, whether it simulates them or shows proxies
	internal static bool IsArbitrating => IsStreaming && GSessionManager.IsServer;

	/// <summary>
	///     Host only: the simulator lent the creation while it's close to another plot's, so collisions between them
	///     are simulated on one machine. A simulator override still comes first.
	/// </summary>
	public int? ArbitratedSimulatorPeerId { get; private set; }

	/// <summary>The creation's simulator apart from arbitration: the override if any, else the owner, else the host.</summary>
	internal int BaseSimulatorPeerId => _simulatorOverride ?? GetOwnerPeerId() ?? HostPeerId;

	internal void SetArbitratedSimulator(int? peerId)
	{
		ArbitratedSimulatorPeerId = peerId;
		UpdateSimulator();
	}
}
