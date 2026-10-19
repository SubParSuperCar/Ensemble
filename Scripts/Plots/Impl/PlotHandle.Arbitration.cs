using EnsembleRoot.Scripts.Plots.Impl;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	// Times an assembly's bounding sphere, so creations change hands before they touch rather than as they do
	private const float ArbitrationRadiusScale = 1.5f;

	// The host alone arbitrates, from the creations it has, whether it simulates them or shows proxies
	internal static bool IsArbitrating => IsStreaming && GSessionManager.IsServer;

	internal ArbitrationSpheres ArbitrationSpheres { get; } = new();

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

	/// <summary>Places a sphere around each assembly where it is; proxies are placed without asking Godot.</summary>
	internal void UpdateArbitrationSpheres()
	{
		ArbitrationSpheres.Clear();

		foreach (var assembly in _assemblies)
		{
			var bounds = assembly.Bounds;

			// Without colliders, it has nothing to collide with
			if (assembly.IsKilled || bounds.Size == Vector3.Zero)
				continue;

			var transform = assembly.IsSimulated ? assembly.GlobalTransform : assembly.ProxyTransform;
			ArbitrationSpheres.Add(transform * bounds.GetCenter(), bounds.Size.Length() / 2 * ArbitrationRadiusScale);
		}
	}
}
