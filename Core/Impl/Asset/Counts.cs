using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Root.Core.Impl.Asset;

public class Counts<TKey> where TKey : notnull
{
	private readonly Dictionary<TKey, int> _byKey = [];

	public IReadOnlyDictionary<TKey, int> All => _byKey;
	public int Total { get; private set; }

	public int Get(TKey key) => _byKey.GetValueOrDefault(key);

	public void Increment(TKey key)
	{
		ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_byKey, key, out _);
		count++;

		Total++;
	}

	public void Decrement(TKey key)
	{
		ref var count = ref CollectionsMarshal.GetValueRefOrNullRef(_byKey, key);

		if (Unsafe.IsNullRef(ref count))
			return;

		if (count <= 1)
			_byKey.Remove(key);
		else
			count--;

		Total--;
	}
}
