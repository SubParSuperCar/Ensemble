using System.Numerics;
using EnsembleCoreRoot.Api.Assets;

#pragma warning disable MA0016

namespace EnsembleRoot.Saving;

public sealed class CreationSaveData
{
	public byte Version { get; init; } = 1;
	public DateTimeOffset UtcCreatedAt { get; init; } = GTimeProvider.GetUtcNow();

	public List<SaveInstance> Instances { get; init; } = [];
}

public sealed class SaveInstance
{
	public ushort AssetId { get; init; }

	public Vector3 Position { get; init; }
	public Quaternion Rotation { get; init; }

	public Dictionary<string, CoreVariant>? Properties { get; init; }
}
