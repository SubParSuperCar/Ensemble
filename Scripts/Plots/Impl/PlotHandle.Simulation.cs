using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	/// <summary>
	///     Bumped whenever the creation's simulators may change (spawning, despawning, hand-offs), on every peer alike,
	///     so streamed state stamped with another epoch is dropped rather than moving the wrong bodies.
	/// </summary>
	public ushort SimulationEpoch { get; private set; }

	// TODO: The plot's owner, once owners simulate their creations (0.54.0)
	/// <summary>The peer that simulates the creation; everyone else shows proxies of it.</summary>
	public int SimulatorPeerId => (int)MultiplayerPeer.TargetPeerServer;

	public bool IsSimulatedLocally => SimulatorPeerId == GSessionManager.LocalPeerId;
}
