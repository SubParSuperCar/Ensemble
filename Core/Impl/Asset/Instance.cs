using System.Numerics;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Instance(IAsset asset, Vector3 position, Quaternion rotation) : IInstance
{
	// Give this an internal setter to be able to update it after its initial construction, but not by outsiders
	public int Id { get; internal set; }

	public IAsset Asset { get; } = asset;
	public IProperties Properties { get; } = new Properties(asset.Properties); // Initialize the properties based on the underlying Asset object

	public Vector3 Position { get; } = position;
	public Quaternion Rotation { get; } = rotation;

	public override string ToString() =>
		$"Instance(instanceId={Id}, assetId={Asset.Id}, position={Position}, rotation={Rotation}, properties={Properties})";
}
