using EnsembleCoreRoot.Api.Assets;

namespace EnsembleRoot.Saving;

public static class SaveConverter
{
	public static CreationSaveData ToSaveData(IInstances instances, TimeProvider timeProvider)
	{
		var save = new CreationSaveData { UtcCreatedAt = timeProvider.GetUtcNow() };

		foreach (var instance in instances.All)
		{
			Dictionary<string, CoreVariant>? properties = null;
			var defaults = instance.Asset.Properties;

			foreach (var (key, value) in instance.Properties.All)
			{
				// Only write properties that differ from the defaults for efficiency
				if (value == defaults[key])
					continue;

				properties ??= new Dictionary<string, CoreVariant>(StringComparer.OrdinalIgnoreCase);
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

	/// <summary>
	///     Adds the saved instances, skipping (for the best chance of recovering data) those whose asset isn't loaded
	///     or whose plot or asset cap is reached, and property values that are unknown or invalid for their property.
	/// </summary>
	public static (int SkippedInstances, int SkippedValues) FromSaveData(IInstances instances, CreationSaveData data)
	{
		var assetIds = instances.GetAllQuotas().Keys.ToHashSet();
		var (skippedInstances, skippedValues) = (0, 0);

		foreach (var instance in data.Instances)
		{
			if (
				!assetIds.Contains(instance.AssetId) ||
				IsLimitReached(instances.Count, instances.MaxCount) ||
				(instances.GetQuota(instance.AssetId) is var (count, maxCount) && IsLimitReached(count, maxCount)))
			{
				skippedInstances++;
				continue;
			}

			var created = instances.Add(instance.AssetId, instance.Position, instance.Rotation);

			if (instance.Properties is not { } properties)
				continue;

			var current = created.Properties.All;

			foreach (var (key, value) in properties)
				if (current.TryGetValue(key, out var existing) && PropertyRules.Check(key, value, existing) is null)
					created.Properties.Update(key, value);
				else
					skippedValues++;
		}

		return (skippedInstances, skippedValues);
	}
}
