// ReSharper disable UnusedMember.Global

namespace EnsembleRoot.Tooling;

public sealed class Clipboard<TKey, TValue>(
	IEqualityComparer<TKey>? keyComparer = null,
	IEqualityComparer<TValue>? valueComparer = null) where TKey : notnull
{
	private readonly Dictionary<TKey, TValue> _contents = new(keyComparer);
	private readonly IEqualityComparer<TValue> _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

	public IReadOnlyDictionary<TKey, TValue> Contents => _contents.AsReadOnly();

	public event Action<TKey, TValue?>? ContentChanged;

	public void Set(TKey key, TValue? value)
	{
		if (TryApply(key, value))
			ContentChanged?.Invoke(key, value);
	}

	public void SetAll(IReadOnlyDictionary<TKey, TValue?> contents)
	{
		var snapshot = contents.ToArray();
		var changes = new List<KeyValuePair<TKey, TValue?>>();

		var keep = snapshot
			.Where(static pair => pair.Value is not null)
			.Select(static pair => pair.Key)
			.ToHashSet(_contents.Comparer);

		foreach (var (key, _) in _contents)
			if (!keep.Contains(key))
			{
				_contents.Remove(key);
				changes.Add(new KeyValuePair<TKey, TValue?>(key, default));
			}

		foreach (var (key, value) in snapshot)
			if (TryApply(key, value))
				changes.Add(new KeyValuePair<TKey, TValue?>(key, value));

		Raise(changes);
	}

	public void Clear()
	{
		if (_contents.Count is 0)
			return;

		var changes = _contents.Keys
			.Select(static key => new KeyValuePair<TKey, TValue?>(key, default))
			.ToArray();

		_contents.Clear();
		Raise(changes);
	}

	private bool TryApply(TKey key, TValue? value)
	{
		if (value is null)
			return _contents.Remove(key);

		if (_contents.TryGetValue(key, out var existing) && _valueComparer.Equals(existing, value))
			return false;

		_contents[key] = value;
		return true;
	}

	private void Raise(IEnumerable<KeyValuePair<TKey, TValue?>> changes)
	{
		foreach (var (key, value) in changes)
			ContentChanged?.Invoke(key, value);
	}
}
