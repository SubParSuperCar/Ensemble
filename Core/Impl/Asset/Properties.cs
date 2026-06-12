using System.Text;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Properties(IReadOnlyDictionary<string, Variant>? values = null) : IProperties
{
	private readonly Dictionary<string, Variant> _values =
		values is null ? [] : new Dictionary<string, Variant>(values, StringComparer.OrdinalIgnoreCase);

	public IReadOnlyDictionary<string, Variant> All => _values;

	public event Action<string, Variant>? Changed;

	public void Update(string key, Variant value)
	{
		if (!_values.TryGetValue(key, out var existing))
			throw new KeyNotFoundException($"Property with key {key} not found");

		if (value == existing)
			return;

		_values[key] = value;
		Changed?.Invoke(key, value);
	}

	public void UpdateAll(IReadOnlyDictionary<string, Variant> values)
	{
		foreach (var (key, value) in values)
			Update(key, value);
	}

	public override string ToString()
	{
		if (_values.Count == 0)
			return "{}";

		var sb = new StringBuilder("{");

		foreach (var (key, value) in _values)
			sb.Append(key).Append(": ").Append(value).Append(", ");

		sb.Length -= 2;
		return sb.Append('}').ToString();
	}
}
