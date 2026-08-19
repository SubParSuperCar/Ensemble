using System.Numerics;

namespace CoreRoot.Api.Assets;

public interface IInstance
{
	int Id { get; }

	IAsset Asset { get; }
	IProperties Properties { get; }

	Vector3 Position { get; }
	Quaternion Rotation { get; }
}
