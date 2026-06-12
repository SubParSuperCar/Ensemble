using System.Globalization;
using System.Runtime.InteropServices;

namespace Root.Core.Impl.Util;

public class HoleyArray<T> where T : class
{
	private readonly List<T?> _items = [];
	private int _next;

	// ReSharper disable once MemberCanBePrivate.Global
	public int Count { get; private set; }

	public event Action<int, T>? Added;
	public event Action<int, T>? Removed;

	public IEnumerable<T> GetAll() => _items.OfType<T>();

	public void Add(T item)
	{
		int index;
		var count = _items.Count;

		if (_next < count)
		{
			index = _next;
			_items[index] = item;
			_next = GetNextFree(_next + 1);
		}
		else
		{
			index = count;
			_items.Add(item);
			_next = count + 1;
		}

		Count++;
		Added?.Invoke(index, item);
	}

	public void AddAt(T item, int index)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index);

		while (_items.Count <= index)
			_items.Add(null);

		if (_items[index] is not null)
			throw new InvalidOperationException(string.Create(
				CultureInfo.InvariantCulture,
				$"Item at index {index} already exists"));

		Count++;
		_items[index] = item;

		if (index == _next)
			_next = GetNextFree(_next + 1);

		Added?.Invoke(index, item);
	}

	public void Remove(int index)
	{
		var item = _items[index] ??
		           throw new InvalidOperationException(string.Create(
			           CultureInfo.InvariantCulture,
			           $"Item at index {index} not found"));

		_items[index] = null;
		Count--;

		if (index < _next)
			_next = index;

		Removed?.Invoke(index, item);
	}

	public bool TryGet(int index, out T item)
	{
		var span = CollectionsMarshal.AsSpan(_items);

		if (index < 0 || index >= span.Length || span[index] is null)
		{
			item = null!;
			return false;
		}

		item = span[index]!;
		return true;
	}

	private int GetNextFree(int current)
	{
		var span = CollectionsMarshal.AsSpan(_items);

		for (var i = current; i < span.Length; i++)
		{
			if (span[i] is null)
				return i;
		}

		return span.Length;
	}
}
