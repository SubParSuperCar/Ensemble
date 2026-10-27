using System.Globalization;
using EnsembleCoreRoot.Api.Assets;
using EnsembleCoreRoot.Api.Plots;
using EnsembleCoreRoot.Assets;

namespace EnsembleCoreRoot.Plots;

/// <inheritdoc />
internal sealed class Plot : IPlot
{
	public Plot(int id, IAssets assets, int? maxOccupantCount = null, int? maxInstanceCount = null)
	{
		Id = id;
		Occupants = new Occupants(this, maxOccupantCount);
		Instances = new Instances(assets, maxInstanceCount);
	}

	public Occupants Occupants { get; }

	public int Id { get; }
	public IInstances Instances { get; }

	IOccupants IPlot.Occupants => Occupants;

	public bool IsSpawned { get; private set; }
	public event Action<bool>? IsSpawnedChanged;

	public void Spawn() => SetIsSpawned(true);
	public void Despawn() => SetIsSpawned(false);

	public void Reset()
	{
		Occupants.Clear();
		Instances.Clear();
		Despawn();
	}

	public override string ToString() =>
		string.Create(CultureInfo.InvariantCulture, $"Plot(id={Id}, isSpawned={IsSpawned})");

	private void SetIsSpawned(bool isSpawned)
	{
		if (isSpawned == IsSpawned)
			return;

		IsSpawned = isSpawned;
		IsSpawnedChanged?.Invoke(isSpawned);
	}
}
