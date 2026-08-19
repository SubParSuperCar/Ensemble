using System.Numerics;
using CoreRoot.Api.Assets;

namespace CoreRoot.Assets;

public class Instance(IAsset asset, Vector3 position, Quaternion rotation) : IInstance
{
	public int Id { get; internal set; }

	public IAsset Asset { get; } = asset;
	public IProperties Properties { get; } = new Properties(asset.Properties);

	public Vector3 Position { get; } = position;
	public Quaternion Rotation { get; } = rotation;

	public override string ToString() =>
		$"Instance(instanceId={Id}, assetId={Asset.Id}, position={Position}, " +
		$"rotation={Rotation}, properties={Properties})";
}
