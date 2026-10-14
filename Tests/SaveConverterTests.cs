using System.Numerics;
using EnsembleCoreRoot;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Saving;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class SaveConverterTests
{
	[Fact]
	public void FromSaveData_SkipsUnknownAssetsCappedInstancesAndInvalidValues()
	{
		var defaults = new Dictionary<string, CoreVariant>(StringComparer.Ordinal)
		{
			["opacity"] = 1d,
			["label"] = "a"
		};

		var core = new Core();
		core.Assets.Add(0, properties: defaults);
		core.Assets.Add(1, maxInstanceCount: 1);

		var instances = core.Plots.Add(0).Instances;
		var data = new CreationSaveData
		{
			Instances =
			{
				Create(7),
				Create(1),
				Create(1),
				Create(0, new Dictionary<string, CoreVariant>(StringComparer.Ordinal)
				{
					["opacity"] = double.NaN,
					["label"] = new string('x', 257),
					["unknown"] = 1,
					["OPACITY"] = 0.5
				})
			}
		};

		var skipped = SaveConverter.FromSaveData(instances, data);

		Assert.Equal((2, 3), skipped);
		Assert.Equal(2, instances.Count);

		var properties = instances.All.Single(static instance => instance.Asset.Id is 0).Properties.All;
		Assert.Equal(new CoreVariant(0.5), properties["opacity"]);
		Assert.Equal(new CoreVariant("a"), properties["label"]);
	}

	private static SaveInstance Create(ushort assetId, Dictionary<string, CoreVariant>? properties = null) =>
		new() { AssetId = assetId, Rotation = Quaternion.Identity, Properties = properties };
}
