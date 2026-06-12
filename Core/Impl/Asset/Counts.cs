using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Root.Core.Impl.Asset;

public class Counts<TKey> where TKey : notnull
{
	private readonly Dictionary<TKey, int> _counts = [];

	// ReSharper disable once UnusedMember.Global
	public IReadOnlyDictionary<TKey, int> All => _counts;
	public int Total { get; private set; }

	public int Get(TKey key) => _counts.GetValueOrDefault(key);

	public void Increment(TKey key)
	{
		Total++;

		ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_counts, key, out _);
		count++;
	}

	public void Decrement(TKey key)
	{
		ref var count = ref CollectionsMarshal.GetValueRefOrNullRef(_counts, key);

		if (Unsafe.IsNullRef(ref count))
			return;

		Total--;

		if (count == 1)
			_counts.Remove(key);
		else
			count--;
	}
}
