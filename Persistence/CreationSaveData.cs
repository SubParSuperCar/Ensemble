using System.Numerics;
using CoreRoot.Api.Assets;

namespace Root.Persistence;

public sealed class CreationSaveData
{
	public ushort Version { get; init; } = 1;
	public DateTimeOffset UtcCreatedAt { get; init; } = DateTimeOffset.UtcNow;

#pragma warning disable MA0016
	public List<SaveInstance> Instances { get; } = [];
#pragma warning restore MA0016
}

public sealed class SaveInstance
{
	public ushort AssetId { get; init; }

	public Vector3 Position { get; init; }
	public Quaternion Rotation { get; init; }

#pragma warning disable MA0016
	public Dictionary<string, CoreVariant>? Properties { get; init; }
#pragma warning restore MA0016
}
