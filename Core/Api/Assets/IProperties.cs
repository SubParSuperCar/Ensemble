namespace EnsembleCoreRoot.Api.Assets;

/// <summary>
///     The property values of an <see cref="IInstance" />, copied from its <see cref="IAsset" />'s defaults.
///     Keys compare by <see cref="KeyComparer" />; only existing keys can be updated, and each keeps its type.
/// </summary>
public interface IProperties
{
	/// <remarks>Case-insensitive, so changing it also changes which saved keys collide.</remarks>
	static StringComparer KeyComparer => StringComparer.OrdinalIgnoreCase;

	IReadOnlyDictionary<string, CoreVariant> All { get; }

	event Action<string, CoreVariant> Changed;

	void Update(string key, CoreVariant value);
	void UpdateAll(IReadOnlyDictionary<string, CoreVariant> values);
}
