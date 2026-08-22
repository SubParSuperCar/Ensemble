using System.Globalization;
using CoreRoot.Api.Assets;

namespace CoreRoot.Assets;

public class Asset(
	int id,
	string? name = null,
	IReadOnlyDictionary<string, CoreVariant>? properties = null,
	int? maxInstanceCount = null)
	: IAsset
{
	private readonly Properties _properties = new(properties);

	public int Id { get; } = id;
	public string Name { get; } = name ?? string.Create(CultureInfo.InvariantCulture, $"Asset {id}");

	public int MaxInstanceCount { get; } = maxInstanceCount ?? -1;

	public IReadOnlyDictionary<string, CoreVariant> Properties => _properties.All;

	// Should we capitalize "id," etc. like in the rest of the codebase (mostly Game)?
	public override string ToString() =>
		$"Asset(id={Id}, name={Name}, maxInstanceCount={MaxInstanceCount}, properties={_properties})";
}
