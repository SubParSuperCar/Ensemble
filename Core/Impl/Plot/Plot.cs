using Root.Core.Api.Asset;
using Root.Core.Api.Plot;
using Root.Core.Impl.Asset;

namespace Root.Core.Impl.Plot;

public class Plot : IPlot
{
	public Plot(int id, IAssets assets, int? maxOccupantCount = null, int? maxInstanceCount = null)
	{
		Id = id;
		Occupants = new Occupants(this, maxOccupantCount);
		Instances = new Instances(assets, maxInstanceCount);
	}

	public Occupants Occupants { get; }
	public IInstances Instances { get; }

	public int Id { get; }

	IOccupants IPlot.Occupants => Occupants;

	public bool IsSpawned { get; private set; }
	public event Action<bool>? IsSpawnedChanged;

	public void Spawn()
	{
		if (IsSpawned)
			return;

		IsSpawned = true;
		IsSpawnedChanged?.Invoke(true);
	}

	public void Despawn()
	{
		if (!IsSpawned)
			return;

		IsSpawned = false;
		IsSpawnedChanged?.Invoke(false);
	}

	public void Reset()
	{
		Occupants.Clear();

		Despawn();
		Instances.Clear();
	}

	public override string ToString() => $"Plot(id={Id}, isSpawned={IsSpawned})";
}
