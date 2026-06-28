using System.Text;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Properties(IReadOnlyDictionary<string, Variant>? values = null) : IProperties
{
	private readonly Dictionary<string, Variant> _valuesByKey = values is null
		? new Dictionary<string, Variant>(StringComparer.OrdinalIgnoreCase)
		: new Dictionary<string, Variant>(values, StringComparer.OrdinalIgnoreCase);

	public IReadOnlyDictionary<string, Variant> All => _valuesByKey;

	public event Action<string, Variant>? Changed;

	public void Update(string key, Variant value)
	{
		if (!_valuesByKey.TryGetValue(key, out var current))
			throw new KeyNotFoundException($"Property with key {key} not found");

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
		if (_valuesByKey.Count == 0)
			return "{}";

		var sb = new StringBuilder("{");

		foreach (var (key, value) in _valuesByKey)
			sb.Append(key).Append(": ").Append(value).Append(", ");

		sb.Length -= 2;
		return sb.Append('}').ToString();
	}
}
