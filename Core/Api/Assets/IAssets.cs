namespace CoreRoot.Api.Assets;

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
