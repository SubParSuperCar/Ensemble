using System.Runtime.InteropServices;

namespace Root.Core.Impl.Util;

public class HoleyArray<T> where T : class
{
	private readonly List<T?> _slots = [];
	private int _lowestFreeSlot;

	// ReSharper disable once MemberCanBePrivate.Global
	public int Count { get; private set; }

	public event Action<int, T>? Added;
	public event Action<int, T>? Removed;

	public IEnumerable<T> GetAll() => _slots.OfType<T>();

	public bool TryGet(int index, out T item)
	{
		var span = CollectionsMarshal.AsSpan(_slots);

		if (index >= 0 && index < span.Length && span[index] is { } found)
		{
			item = found;
			return true;
		}

		item = null!;
		return false;
	}

	public void Add(T item)
	{
		var slot = _lowestFreeSlot;
		Insert(item, slot);

		_lowestFreeSlot = ScanForFreeSlot(slot + 1);
		Added?.Invoke(slot, item);
	}

	public void AddAt(T item, int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);

		if (TryGet(index, out _))
			throw new InvalidOperationException($"Item at index {index} already exists");

		Insert(item, index);

		if (index == _lowestFreeSlot)
			_lowestFreeSlot = ScanForFreeSlot(index + 1);

		Added?.Invoke(index, item);
	}

	public void Remove(int index)
	{
		if (!TryGet(index, out var item))
			throw new InvalidOperationException($"Item at index {index} not found");

		_slots[index] = null;
		Count--;

		if (index < _lowestFreeSlot)
			_lowestFreeSlot = index;

		Removed?.Invoke(index, item);
	}

	private void Insert(T item, int index)
	{
		while (_slots.Count <= index)
			_slots.Add(null);

		_slots[index] = item;
		Count++;
	}

	private int ScanForFreeSlot(int from)
	{
		var span = CollectionsMarshal.AsSpan(_slots);

		for (var index = from; index < span.Length; index++)
		{
			if (span[index] is null)
				return index;
		}

		return span.Length;
	}
}
