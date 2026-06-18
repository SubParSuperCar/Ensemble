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

	public int MaxInstanceCount { get; } = maxInstanceCount ?? Unlimited;

	public IReadOnlyDictionary<string, Variant> Properties => _properties.All;

	public override string ToString()
		=> $"Asset(id={Id}, name={Name}, maxInstanceCount={MaxInstanceCount}, properties={_properties})";
}
