namespace EnsembleCoreRoot.Api.Assets;

/// <summary>
///     The registry of <see cref="IAsset" /> objects. Should be locked once registration is complete.
/// </summary>
public interface IAssets
{
	IReadOnlyDictionary<int, IAsset> All { get; }
	bool IsLocked { get; }

	event Action<IAsset> Added;
	event Action<IAsset> Removed;

	IAsset Add(
		int id,
		string? name = null,
		IReadOnlyDictionary<string, CoreVariant>? properties = null,
		int? maxInstanceCount = null);

	void Lock();
}
