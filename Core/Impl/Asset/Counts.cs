using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Root.Core.Impl.Asset;

internal sealed class Counts<TKey> where TKey : notnull
{
	private readonly Dictionary<TKey, int> _countsByKey = [];

	// ReSharper disable once UnusedMember.Global
	// There are some unused members kept for the sake of correctness and future-proofing. This has negligible cost.
	public IReadOnlyDictionary<TKey, int> All => _countsByKey;
	public int Total { get; private set; }

	public int Get(TKey key) => _countsByKey.GetValueOrDefault(key);

	public void Increment(TKey key)
	{
		// Fancy and optimized way to increment the count
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
