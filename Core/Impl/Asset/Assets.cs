using System.Globalization;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Assets : IAssets
{
	private readonly Dictionary<int, IAsset> _assets = [];

	public IReadOnlyDictionary<int, IAsset> All => _assets;
	public bool IsLocked { get; private set; }

	public event Action<IAsset>? Added;

	public IAsset Add(
		int id,
		string? name = null,
		IReadOnlyDictionary<string, Variant>? properties = null,
		int? maxInstanceCount = null)
	{
		if (IsLocked)
			throw new InvalidOperationException("Assets registry is locked");

		ArgumentOutOfRangeException.ThrowIfNegative(id);

		if (maxInstanceCount.HasValue)
			ArgumentOutOfRangeException.ThrowIfNegative(maxInstanceCount.Value);

		if (_assets.ContainsKey(id))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {id} already exists"));

		var asset = new Asset(id, name, properties, maxInstanceCount);
		_assets.Add(id, asset);
		Added?.Invoke(asset);

		return asset;
	}

	public void Lock() => IsLocked = true;

	public void Reset()
	{
		_assets.Clear();
		IsLocked = false;
	}
}
