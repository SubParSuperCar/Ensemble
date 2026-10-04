using System.Text;
using EnsembleCoreRoot.Api.Assets;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace EnsembleRoot.Saving.SerDes;

/// <remarks>
///     Counts are stored plus one, and floats must be finite, so all-zero and all-one values (the most likely
///     corruption) are rejected.
/// </remarks>
public sealed class BinarySaveSerializer : ISaveSerializer
{
	private const byte FormatVersion = 0;
	private const int MaxPreallocatedCount = 1 << 12;

	private static ReadOnlySpan<byte> Magic => "ENSB"u8;

	public void Serialize(Stream stream, CreationSaveData data)
	{
		using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

		writer.Write(Magic);
		writer.Write(FormatVersion);

		writer.Write(data.Version);
		writer.Write(data.UtcCreatedAt.UtcTicks);

		writer.Write(Math.Min(data.Instances.Count, int.MaxValue - 1) + 1);

		foreach (var instance in data.Instances)
		{
			writer.Write(instance.AssetId);

			writer.Write(instance.Position.X);
			writer.Write(instance.Position.Y);
			writer.Write(instance.Position.Z);

			writer.Write(instance.Rotation.X);
			writer.Write(instance.Rotation.Y);
			writer.Write(instance.Rotation.Z);
			writer.Write(instance.Rotation.W);

			var properties = instance.Properties;
			var propertyCount = Math.Min(properties?.Count ?? 0, ushort.MaxValue - 2) + 1;
			writer.Write((ushort)propertyCount);

			if (properties is null)
				continue;

			foreach (var (key, value) in properties)
			{
				writer.Write(key);
				CoreVariantSerializer.Write(writer, value);
			}
		}
	}

#pragma warning disable MA0051
	public CreationSaveData Deserialize(Stream stream)
#pragma warning restore MA0051
	{
		using var reader = new BinaryReader(stream, Encoding.UTF8, true);

		var magic = reader.ReadBytes(Magic.Length);
		if (!magic.AsSpan().SequenceEqual(Magic))
			throw new InvalidDataException("Invalid save file.");

		var formatVersion = reader.ReadByte();
		if (formatVersion is not FormatVersion)
			throw new InvalidDataException($"Unsupported save format version: {formatVersion}.");

		var version = reader.ReadByte();

		var utcTicks = reader.ReadInt64();
		if (utcTicks <= DateTimeOffset.MinValue.UtcTicks || utcTicks > DateTimeOffset.MaxValue.UtcTicks)
			throw new InvalidDataException("Invalid creation time.");

		var save = new CreationSaveData
		{
			Version = version,
			UtcCreatedAt = new DateTimeOffset(utcTicks, TimeSpan.Zero)
		};

		var instanceCount = reader.ReadInt32();
		if (instanceCount-- <= 0)
			throw new InvalidDataException("Invalid instance count.");

		save.Instances.Capacity = Math.Min(instanceCount, MaxPreallocatedCount);

		for (var i = 0; i < instanceCount; i++)
		{
			var assetId = reader.ReadUInt16();

			var position = new Vector3(
				ReadFiniteSingle(reader),
				ReadFiniteSingle(reader),
				ReadFiniteSingle(reader));

			var rotation = new Quaternion(
				ReadFiniteSingle(reader),
				ReadFiniteSingle(reader),
				ReadFiniteSingle(reader),
				ReadFiniteSingle(reader));

			if (rotation.LengthSquared() is 0f)
				throw new InvalidDataException("Invalid rotation.");

			var propertyCount = reader.ReadUInt16();
			if (propertyCount-- is 0 or ushort.MaxValue)
				throw new InvalidDataException("Invalid property count.");

			Dictionary<string, CoreVariant>? properties = null;

			if (propertyCount is not 0)
			{
				properties = new Dictionary<string, CoreVariant>(
					Math.Min((int)propertyCount, MaxPreallocatedCount),
					StringComparer.Ordinal);

				for (var j = 0; j < propertyCount; j++)
				{
					var key = reader.ReadString();
					var value = CoreVariantSerializer.Read(reader);

					properties.Add(key, value);
				}
			}

			save.Instances.Add(new SaveInstance
			{
				AssetId = assetId,
				Position = position,
				Rotation = rotation,
				Properties = properties
			});
		}

		return save;
	}

	private static float ReadFiniteSingle(BinaryReader reader) =>
		reader.ReadSingle() is var value && float.IsFinite(value)
			? value
			: throw new InvalidDataException("Invalid number.");
}
