namespace CoreRoot.Api.Assets;

/// <summary>
///     The property values of an <see cref="IInstance" />, copied from its <see cref="IAsset" />'s defaults.
///     Keys are case-insensitive; only existing keys can be updated.
/// </summary>
public interface IProperties
{
	IReadOnlyDictionary<string, CoreVariant> All { get; }

	event Action<string, CoreVariant> Changed;

	void Update(string key, CoreVariant value);
	void UpdateAll(IReadOnlyDictionary<string, CoreVariant> values);
}
