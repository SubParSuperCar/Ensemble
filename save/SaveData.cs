using System.Numerics;
using Root.Core.Api.Asset;

namespace Root.Save;

public sealed class SaveData
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
	public Dictionary<string, Variant>? Properties { get; init; }
#pragma warning restore MA0016
}
