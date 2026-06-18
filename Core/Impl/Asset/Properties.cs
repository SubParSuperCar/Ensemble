using System.Text;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Properties(IReadOnlyDictionary<string, Variant>? values = null) : IProperties
{
	private readonly Dictionary<string, Variant> _byKey = values is null
		? new Dictionary<string, Variant>(StringComparer.OrdinalIgnoreCase)
		: new Dictionary<string, Variant>(values, StringComparer.OrdinalIgnoreCase);

	public IReadOnlyDictionary<string, Variant> All => _byKey;

	public event Action<string, Variant>? Changed;

	public void Update(string key, Variant value)
	{
		if (!_byKey.TryGetValue(key, out var current))
			throw new KeyNotFoundException($"Property with key {key} not found");

		if (current == value)
			return;

		_byKey[key] = value;
		Changed?.Invoke(key, value);
	}

	public void UpdateAll(IReadOnlyDictionary<string, Variant> values)
	{
		foreach (var (key, value) in values)
			Update(key, value);
	}

	public override string ToString()
	{
		if (_byKey.Count == 0)
			return "{}";

		var text = new StringBuilder("{");

		foreach (var (key, value) in _byKey)
			text.Append(key).Append(": ").Append(value).Append(", ");

		text.Length -= 2;
		return text.Append('}').ToString();
	}
}
