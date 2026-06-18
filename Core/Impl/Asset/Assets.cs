using System.Globalization;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Assets : IAssets
{
	private readonly Dictionary<int, IAsset> _byId = [];

	public IReadOnlyDictionary<int, IAsset> All => _byId;
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

		if (maxInstanceCount is { } count)
			ArgumentOutOfRangeException.ThrowIfNegative(count);

		if (_byId.ContainsKey(id))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {id} already exists"));

		var asset = new Asset(id, name, properties, maxInstanceCount);
		_byId.Add(id, asset);

		Added?.Invoke(asset);
		return asset;
	}

	public void Lock() => IsLocked = true;
}
