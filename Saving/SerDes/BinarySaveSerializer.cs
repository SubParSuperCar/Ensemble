using System.Globalization;
using System.Numerics;
using System.Text;
using EnsembleCoreRoot.Api.Assets;

namespace EnsembleRoot.Saving.SerDes;

/// <remarks>
///     Counts are stored plus one, and floats must be finite, so all-zero and all-one values (the most likely
///     corruption) are rejected.
/// </remarks>
public sealed class BinarySaveSerializer : ISaveSerializer
{
	private const byte FormatVersion = 0;
	private const int MaxPreallocatedCount = 1 << 12;
	private const int MaxPropertyCount = ushort.MaxValue - 2;

	private static ReadOnlySpan<byte> Magic => "ENSB"u8;

	public void Serialize(Stream stream, CreationSaveData data)
	{
		using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

		writer.Write(Magic);
		writer.Write(FormatVersion);

		writer.Write(data.Version);
		writer.Write(data.UtcCreatedAt.UtcTicks);

		writer.Write(data.Instances.Count + 1);

		foreach (var instance in data.Instances)
			WriteInstance(writer, instance);
	}

	public CreationSaveData Deserialize(Stream stream)
	{
		using var reader = new BinaryReader(stream, Encoding.UTF8, true);

		if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic))
			throw new InvalidDataException("Unrecognized save data.");

		var formatVersion = reader.ReadByte();
		if (formatVersion is not FormatVersion)
			throw new InvalidDataException($"Unsupported save format version: {formatVersion}.");

		var version = reader.ReadByte();

		var utcTicks = reader.ReadInt64();
		if (utcTicks <= DateTimeOffset.MinValue.UtcTicks || utcTicks > DateTimeOffset.MaxValue.UtcTicks)
			throw new InvalidDataException("Invalid creation time.");

		var instanceCount = reader.ReadInt32();
		if (instanceCount-- <= 0)
			throw new InvalidDataException("Invalid instance count.");

		var save = new CreationSaveData
		{
			Version = version,
			UtcCreatedAt = new DateTimeOffset(utcTicks, TimeSpan.Zero),
			Instances = new List<SaveInstance>(Math.Min(instanceCount, MaxPreallocatedCount))
		};

		for (var i = 0; i < instanceCount; i++)
			save.Instances.Add(ReadInstance(reader));

		return save;
	}

	private static void WriteInstance(BinaryWriter writer, SaveInstance instance)
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
		var propertyCount = properties?.Count ?? 0;

		if (propertyCount > MaxPropertyCount)
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Instance has {propertyCount} properties, but at most {MaxPropertyCount} are supported."));

		writer.Write((ushort)(propertyCount + 1));

		if (properties is null)
			return;

		foreach (var (key, value) in properties)
		{
			writer.Write(key);
			CoreVariantSerializer.Write(writer, value);
		}
	}

	private static SaveInstance ReadInstance(BinaryReader reader)
	{
		var assetId = reader.ReadUInt16();

		var position = new Vector3(ReadFiniteSingle(reader), ReadFiniteSingle(reader), ReadFiniteSingle(reader));

		var rotation = new Quaternion(
			ReadFiniteSingle(reader), ReadFiniteSingle(reader), ReadFiniteSingle(reader), ReadFiniteSingle(reader));

		if (rotation.LengthSquared() is 0f)
			throw new InvalidDataException("Invalid rotation.");

		return new SaveInstance
		{
			AssetId = assetId,
			Position = position,
			Rotation = rotation,
			Properties = ReadProperties(reader)
		};
	}

	private static Dictionary<string, CoreVariant>? ReadProperties(BinaryReader reader)
	{
		var propertyCount = reader.ReadUInt16();
		if (propertyCount-- is 0 or ushort.MaxValue)
			throw new InvalidDataException("Invalid property count.");

		if (propertyCount is 0)
			return null;

		var properties = new Dictionary<string, CoreVariant>(
			Math.Min((int)propertyCount, MaxPreallocatedCount), StringComparer.Ordinal);

		for (var i = 0; i < propertyCount; i++)
		{
			var key = reader.ReadString();
			var value = CoreVariantSerializer.Read(reader);

			if (!properties.TryAdd(key, value))
				throw new InvalidDataException($"Duplicate property key: {key}.");
		}

		return properties;
	}

	private static float ReadFiniteSingle(BinaryReader reader) =>
		reader.ReadSingle() is var value && float.IsFinite(value)
			? value
			: throw new InvalidDataException("Invalid floating-point value.");
}
