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
		ThrowIfInvalid(key, value);

		if (_valuesByKey[key] == value)
			return;

		_valuesByKey[key] = value;
		Changed?.Invoke(key, value);
	}

	public void UpdateAll(IReadOnlyDictionary<string, CoreVariant> values)
	{
		// Checked first, so an invalid value leaves every property as it was
		foreach (var (key, value) in values)
			ThrowIfInvalid(key, value);

		foreach (var (key, value) in values)
			Update(key, value);
	}

	public override string ToString() => Format(_valuesByKey);

	private void ThrowIfInvalid(string key, CoreVariant value)
	{
		if (!_valuesByKey.TryGetValue(key, out var current))
			throw new KeyNotFoundException($"Property with key {key} not found.");

		if (PropertyRules.Check(key, value, current) is { } reason)
			throw new ArgumentException(reason, nameof(value));
	}

	internal static string Format(IEnumerable<KeyValuePair<string, CoreVariant>> values) =>
		"{" + string.Join(", ", values.Select(static pair => $"{pair.Key}: {pair.Value}")) + "}";
}
