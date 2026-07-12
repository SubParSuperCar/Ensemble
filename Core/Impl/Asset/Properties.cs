using System.Text;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Properties(IReadOnlyDictionary<string, Variant>? values = null) : IProperties
{
	// If no initial values are provided, default to an empty dictionary. Prefer collection expression.
	private readonly Dictionary<string, Variant> _valuesByKey =
		values is null ? [] : new Dictionary<string, Variant>(values, StringComparer.OrdinalIgnoreCase);

	public IReadOnlyDictionary<string, Variant> All => _valuesByKey;

	public event Action<string, Variant>? Changed;

	public void Update(string key, Variant value)
	{
		// Only update the property if it exists. The caller should not be able to make up new property keys.
		if (!_valuesByKey.TryGetValue(key, out var current))
			throw new KeyNotFoundException($"Property with key {key} not found");

		// Only update the property if it's actually different
		// TODO: For comparisons, make which side each value is on deterministic
		if (current == value)
			return;

		_valuesByKey[key] = value;
		Changed?.Invoke(key, value);
	}

	public void UpdateAll(IReadOnlyDictionary<string, Variant> values)
	{
		foreach (var (key, value) in values)
			Update(key, value);
	}

	public override string ToString()
	{
		// Utilize a StringBuilder for optimization. The property table may be large.
		if (_valuesByKey.Count == 0)
			return "{}";

		var sb = new StringBuilder("{");

		foreach (var (key, value) in _valuesByKey)
			sb.Append(key).Append(": ").Append(value).Append(", ");

		sb.Length -= 2;
		return sb.Append('}').ToString();
	}
}
