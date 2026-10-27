using System.Collections.Frozen;
using EnsembleCoreRoot.Api.Assets;

namespace EnsembleCoreRoot.Assets;

/// <inheritdoc />
internal sealed class Properties(IReadOnlyDictionary<string, CoreVariant>? values = null) : IProperties
{
	private readonly Dictionary<string, CoreVariant> _valuesByKey =
		new(values ?? FrozenDictionary<string, CoreVariant>.Empty, StringComparer.OrdinalIgnoreCase);

	public IReadOnlyDictionary<string, CoreVariant> All => _valuesByKey;

	public event Action<string, CoreVariant>? Changed;

	public void Update(string key, CoreVariant value)
	{
		if (!_valuesByKey.TryGetValue(key, out var current))
			throw new KeyNotFoundException($"Property with key {key} not found.");

		if (current == value)
			return;

		_valuesByKey[key] = value;
		Changed?.Invoke(key, value);
	}

	public void UpdateAll(IReadOnlyDictionary<string, CoreVariant> values)
	{
		foreach (var (key, value) in values)
			Update(key, value);
	}

	public override string ToString() => Format(_valuesByKey);

	internal static string Format(IEnumerable<KeyValuePair<string, CoreVariant>> values) =>
		"{" + string.Join(", ", values.Select(static pair => $"{pair.Key}: {pair.Value}")) + "}";
}
