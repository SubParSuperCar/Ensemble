using System.Globalization;
using System.Runtime.InteropServices;

namespace Root.Core.Impl.Util;

public class HoleyArray<T> where T : class
{
	private readonly List<T?> _slots = [];
	private int _nextFreeIndex;

	// ReSharper disable once MemberCanBePrivate.Global
	public int Count { get; private set; }

	public event Action<int, T>? Added;
	public event Action<int, T>? Removed;

	public IEnumerable<T> GetAll() => _slots.OfType<T>();

	public void Add(T item)
	{
		var index = _nextFreeIndex;

		if (index < _slots.Count)
			_slots[index] = item;
		else
			_slots.Add(item);

		_nextFreeIndex = FindNextFree(index + 1);

		Count++;
		Added?.Invoke(index, item);
	}

	public void AddAt(T item, int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);

		while (_slots.Count <= index)
			_slots.Add(null);

		if (_slots[index] is not null)
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Item at index {index} already exists"));

		_slots[index] = item;
		Count++;

		if (index == _nextFreeIndex)
			_nextFreeIndex = FindNextFree(index + 1);

		Added?.Invoke(index, item);
	}

	public void Remove(int index)
	{
		var item = _slots[index] ??
				   throw new InvalidOperationException(string.Create(
					   CultureInfo.InvariantCulture,
					   $"Item at index {index} not found"));

		_slots[index] = null;
		Count--;

		if (index < _nextFreeIndex)
			_nextFreeIndex = index;

		Removed?.Invoke(index, item);
	}

	public bool TryGet(int index, out T item)
	{
		var slots = CollectionsMarshal.AsSpan(_slots);

		if (index >= 0 && index < slots.Length && slots[index] is { } found)
		{
			item = found;
			return true;
		}

		item = null!;
		return false;
	}

	private int FindNextFree(int from)
	{
		var slots = CollectionsMarshal.AsSpan(_slots);

		for (var i = from; i < slots.Length; i++)
		{
			if (slots[i] is null)
				return i;
		}

		return slots.Length;
	}
}
