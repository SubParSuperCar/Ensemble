namespace CoreRoot.Api.Assets;

public interface IProperties
{
	IReadOnlyDictionary<string, CoreVariant> All { get; }

	event Action<string, CoreVariant> Changed;

	void Update(string key, CoreVariant value);
	void UpdateAll(IReadOnlyDictionary<string, CoreVariant> values);
}
