namespace EnsembleCoreRoot.Api.Assets;

/// <summary>
///     The representation or template of a placeable asset.
///     In Ensemble, "asset" is a formal synonym of "block".
/// </summary>
public interface IAsset
{
	/// Asset ID.
	int Id { get; }

	string Name { get; }

	int MaxInstanceCount { get; }

	/// Read-only until copied to an
	/// <see cref="IInstance" />
	/// object.
	IReadOnlyDictionary<string, CoreVariant> Properties { get; }
}
