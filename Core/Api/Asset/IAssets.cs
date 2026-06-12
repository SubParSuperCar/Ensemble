namespace Root.Core.Api.Asset;

public interface IAssets
{
	IReadOnlyDictionary<int, IAsset> All { get; }
	bool IsLocked { get; }

	IAsset Add(
		int id,
		string? name = null,
		IReadOnlyDictionary<string, Variant>? properties = null,
		int? maxInstanceCount = null);

	event Action<IAsset> Added;

	void Lock();
	void Reset();
}
