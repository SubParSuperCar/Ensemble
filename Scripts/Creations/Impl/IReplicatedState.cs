namespace EnsembleRoot.Scripts.Creations;

/// <summary>
///     Live state that must outlast its simulator: the simulator decides it, and every peer keeps it, as any of them
///     may simulate next or may have to show it. It reaches them reliably, unlike the stream.
/// </summary>
public interface IReplicatedState
{
	/// <remarks>Any value may arrive from a hostile simulator.</remarks>
	int State { get; set; }
}
