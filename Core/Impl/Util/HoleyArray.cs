using System.Globalization;
using System.Runtime.InteropServices;

namespace Root.Core.Impl.Util;

internal sealed class HoleyArray<T> where T : class
{
	private readonly List<T?> _items = [];
	private int _nextFreeIndex;

	// ReSharper disable once MemberCanBePrivate.Global
	public int Count { get; private set; }

	public event Action<int, T>? Added;
	public event Action<int, T>? Removed;

	public IEnumerable<T> GetAll() => _items.OfType<T>();

	public bool TryGet(int index, out T item)
	{
		var items = CollectionsMarshal.AsSpan(_items);

		if (index >= 0 && index < items.Length && items[index] is { } found)
		{
			item = found;
			return true;
		}

		item = null!;
		return false;
	}

	public void Add(T item)
	{
		var index = _nextFreeIndex;
		Place(item, index);

		_nextFreeIndex = FindFreeIndex(index + 1);
		Added?.Invoke(index, item);
	}

	public void AddAt(T item, int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);

		if (TryGet(index, out _))
			throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
				$"Item at index {index} already exists"));

		Place(item, index);

		if (index == _nextFreeIndex)
			_nextFreeIndex = FindFreeIndex(index + 1);

		Added?.Invoke(index, item);
	}

	public void Remove(int index)
	{
		if (!TryGet(index, out var item))
			throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
				$"Item at index {index} not found"));

		_items[index] = null;
		Count--;

		if (index < _nextFreeIndex)
			_nextFreeIndex = index;

		Removed?.Invoke(index, item);
	}

	private void Place(T item, int index)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(index, ushort.MaxValue);

		while (_items.Count <= index)
			_items.Add(null);

		_items[index] = item;
		Count++;
	}

	private int FindFreeIndex(int from)
	{
		var items = CollectionsMarshal.AsSpan(_items);

		for (var index = from; index < items.Length; index++)
		{
			if (items[index] is null)
				return index;
		}

		return items.Length;
	}
}
