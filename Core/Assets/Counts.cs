using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CoreRoot.Assets;

internal sealed class Counts<TKey> where TKey : notnull
{
	private readonly Dictionary<TKey, int> _countsByKey = [];

	// ReSharper disable once UnusedMember.Global
	public IReadOnlyDictionary<TKey, int> All => _countsByKey;
	public int Total { get; private set; }

	public int Get(TKey key) => _countsByKey.GetValueOrDefault(key);

	public void Increment(TKey key)
	{
		ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_countsByKey, key, out _);
		count++;

		Total++;
	}

	public void Decrement(TKey key)
	{
		ref var count = ref CollectionsMarshal.GetValueRefOrNullRef(_countsByKey, key);

		if (Unsafe.IsNullRef(ref count))
			return;

		if (count <= 1)
			_countsByKey.Remove(key);
		else
			count--;

		Total--;
	}
}
