using System.Numerics;
using EnsembleCoreRoot;
using EnsembleCoreRoot.Api.Assets;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class InstancesTests
{
	[Fact]
	public void Add_AssignsLowestFreeId()
	{
		var instances = CreateInstances();

		for (var i = 0; i < 3; i++)
			Add(instances);

		instances.Remove(1);

		Assert.Equal(1, Add(instances).Id);
		Assert.Equal(3, Add(instances).Id);
	}

	[Fact]
	public void Add_SkipsIdsTakenByAddAt()
	{
		var instances = CreateInstances();
		instances.Add(0, Vector3.Zero, Quaternion.Identity, 1);

		Assert.Equal(0, Add(instances).Id);
		Assert.Equal(2, Add(instances).Id);
	}

	[Fact]
	public void AddAndRemove_TrackQuotas()
	{
		var instances = CreateInstances();

		Add(instances);
		Add(instances);
		instances.Remove(0);

		Assert.Equal((1, 2), instances.GetQuota(0));
		Assert.Equal(1, instances.Count);
	}

	[Fact]
	public void AssetProperties_IgnoreKeyCase()
	{
		var core = new Core();
		var asset = core.Assets.Add(0, null, new Dictionary<string, CoreVariant> { ["Color"] = CoreVariant.Null });

		Assert.True(asset.Properties.ContainsKey("color"));
	}

	private static IInstances CreateInstances()
	{
		var core = new Core();
		core.Assets.Add(0, maxInstanceCount: 2);

		return core.Plots.Add(0).Instances;
	}

	private static IInstance Add(IInstances instances) => instances.Add(0, Vector3.Zero, Quaternion.Identity);
}
