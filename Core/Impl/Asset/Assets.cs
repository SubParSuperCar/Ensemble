using System.Globalization;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Assets : IAssets
{
	private readonly Dictionary<int, IAsset> _assetsById = [];

	public IReadOnlyDictionary<int, IAsset> All => _assetsById;
	public bool IsLocked { get; private set; }

	public event Action<IAsset>? Added;
	public event Action<IAsset>? Removed;

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

		if (_assetsById.ContainsKey(id))
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Asset with id {id} already exists"));

		var asset = new Asset(id, name, properties, maxInstanceCount);
		_assetsById.Add(id, asset);
		Added?.Invoke(asset);

		return asset;
	}

	public void Lock() => IsLocked = true;

	// An internal method for resetting everything when the session ends
	// This is a more efficient method than implementing IDisposable and/or having to update GdCore
	internal void Reset()
	{
		foreach (var (id, asset) in _assetsById.ToArray())
		{
			_assetsById.Remove(id);
			Removed?.Invoke(asset);
		}

		IsLocked = false;
	}
}
