using EnsembleCoreRoot.Api.Assets;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EnsembleRoot.Saving;

public static class SaveConverter
{
	public static CreationSaveData ToSaveData(IInstances instances)
	{
		var save = new CreationSaveData();

		foreach (var instance in instances.All)
		{
			Dictionary<string, CoreVariant>? properties = null;
			var defaults = instance.Asset.Properties;

			foreach (var (key, value) in instance.Properties.All)
			{
				if (value == defaults[key])
					continue;

				properties ??= new Dictionary<string, CoreVariant>(StringComparer.Ordinal);
				properties.Add(key, value);
			}

			save.Instances.Add(new SaveInstance
			{
				AssetId = checked((ushort)instance.Asset.Id),
				Position = instance.Position,
				Rotation = instance.Rotation,
				Properties = properties
			});
		}

		return save;
	}

	public static void FromSaveData(IInstances instances, CreationSaveData data)
	{
		foreach (var instance in data.Instances)
		{
			var created = instances.Add(instance.AssetId, instance.Position, instance.Rotation);

			if (instance.Properties is not { } properties)
				continue;

			foreach (var (key, value) in properties)
				if (created.Properties.All.ContainsKey(key))
					created.Properties.Update(key, value);
		}
	}
}
