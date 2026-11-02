namespace EnsembleRoot.Scripts.Creations;

/// <summary>
///     Physics logic of an <see cref="Assets.AssetHandle" /> or its <see cref="Assets.AssetComponent" />, ticked in
///     instance ID order on the peer simulating its creation, so it never checks who simulates. Other peers' copies
///     simply follow the stream, and a new simulator's ticks take over after a hand-off.
/// </summary>
public interface ISimulationTicked
{
	void Tick(in SimulationTick tick);
}
