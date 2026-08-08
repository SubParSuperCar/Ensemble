using Root.Core.Api.Asset;
using Root.Core.Impl.Asset;

// ReSharper disable UnusedMember.Global

namespace Root.Save;

// ReSharper disable once UnusedType.Global
public sealed class SaveConverter
{
	public static SaveData ToSaveData(Instances instances)
	{
		var save = new SaveData();

		foreach (var instance in instances.All)
		{
			Dictionary<string, Variant>? properties = null;
			var defaults = instance.Asset.Properties;

			foreach (var (key, value) in instance.Properties.All)
			{
				if (value == defaults[key])
					continue;

				properties ??= new Dictionary<string, Variant>(StringComparer.Ordinal);
				properties.Add(key, value);
			}

			save.Instances.Add(new SaveInstance
			{
				AssetId = (ushort)instance.Asset.Id,
				Position = instance.Position,
				Rotation = instance.Rotation,
				Properties = properties
			});
		}

		return save;
	}

	public static void FromSaveData(Instances instances, SaveData data)
	{
		foreach (var instance in data.Instances)
		{
			var created = instances.Add(
				instance.AssetId,
				instance.Position,
				instance.Rotation);

			if (instance.Properties is null)
				continue;

			foreach (var (key, value) in instance.Properties)
				created.Properties.Update(key, value);
		}
	}
}
