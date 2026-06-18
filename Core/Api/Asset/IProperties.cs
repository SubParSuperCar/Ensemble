namespace Root.Core.Api.Asset;

public interface IProperties
{
	IReadOnlyDictionary<string, Variant> All { get; }

	event Action<string, Variant> Changed;

	void Update(string key, Variant value);
	void UpdateAll(IReadOnlyDictionary<string, Variant> values);
}
