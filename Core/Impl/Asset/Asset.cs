using System.Globalization;
using Root.Core.Api.Asset;

namespace Root.Core.Impl.Asset;

public class Asset(
	int id,
	string? name = null,
	IReadOnlyDictionary<string, Variant>? properties = null,
	int? maxInstanceCount = null)
	: IAsset
{
	private readonly Properties _properties = new(properties);

	public int Id { get; } = id;
	public string Name { get; } = name ?? string.Create(CultureInfo.InvariantCulture, $"Asset {id}");

	// Fallback to a sentinel value representing unlimited/infinity when no limit is defined. A similar paradigm is seen throughout.
	// TODO: Should this and similar primitive items be handled in the parameters section or at the assignment?
	public int MaxInstanceCount { get; } = maxInstanceCount ?? Unlimited;

	// Store the properties as an immutable dictionary until they become active as an IInstance object
	public IReadOnlyDictionary<string, Variant> Properties => _properties.All;

	// Call ToString on the underlying IProperties object instead of the wrapper
	public override string ToString() =>
		$"Asset(id={Id}, name={Name}, maxInstanceCount={MaxInstanceCount}, properties={_properties})";
}
