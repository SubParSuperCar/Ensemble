namespace Root.Core.Api.Asset;

public interface IAssets
{
	IReadOnlyDictionary<int, IAsset> All { get; }
	bool IsLocked { get; }

	event Action<IAsset> Added;
	event Action<IAsset> Removed;

	// TODO: For defaulting parameters like 'maxInstanceCount', should the default be defined in the parameters or inside the constructor?
	IAsset Add(
		int id,
		string? name = null,
		IReadOnlyDictionary<string, Variant>? properties = null,
		int? maxInstanceCount = null);

	void Lock();
}
