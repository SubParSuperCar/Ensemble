using System.Numerics;

namespace EnsembleCoreRoot.Api.Assets;

/// <summary>
///     The representation of a placed asset.
/// </summary>
public interface IInstance
{
	/// Instance ID.
	int Id { get; }

	/// <inheritdoc cref="IAsset" />
	IAsset Asset { get; }

	/// <inheritdoc cref="IProperties" />
	IProperties Properties { get; }

	Vector3 Position { get; }
	Quaternion Rotation { get; }
}
