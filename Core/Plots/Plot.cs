using System.Globalization;
using EnsembleCoreRoot.Api.Assets;
using EnsembleCoreRoot.Api.Plots;
using EnsembleCoreRoot.Assets;

namespace EnsembleCoreRoot.Plots;

/// <inheritdoc />
public sealed class Plot : IPlot
{
	public Plot(int id, IAssets assets, int? maxOccupantCount = null, int? maxInstanceCount = null)
	{
		Id = id;
		Occupants = new Occupants(this, maxOccupantCount);
		Instances = new Instances(assets, maxInstanceCount);
	}

	// Rider's Code Cleanup moves Occupants apart from Instances; don't fight it
	public Occupants Occupants { get; }

	public int Id { get; }
	public IInstances Instances { get; }

	IOccupants IPlot.Occupants => Occupants;

	public bool IsSpawned { get; private set; }
	public event Action<bool>? IsSpawnedChanged;

	public void Spawn()
	{
		if (IsSpawned)
			return;

		IsSpawned = true;
		IsSpawnedChanged?.Invoke(IsSpawned);
	}

	public void Despawn()
	{
		if (!IsSpawned)
			return;

		IsSpawned = false;
		IsSpawnedChanged?.Invoke(IsSpawned);
	}

	public void Reset()
	{
		Occupants.Clear();

		Despawn();
		Instances.Clear();
	}

	public override string ToString() =>
		string.Create(CultureInfo.InvariantCulture, $"Plot(id={Id}, isSpawned={IsSpawned})");
}
