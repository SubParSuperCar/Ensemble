using System.Collections.Frozen;
using System.Globalization;
using CoreRoot.Api.Assets;

namespace CoreRoot.Assets;

public class Asset(
	int id,
	string? name = null,
	FrozenDictionary<string, CoreVariant>? properties = null,
	int? maxInstanceCount = null)
	: IAsset
{
	public int Id { get; } = id;
	public string Name { get; } = name ?? string.Create(CultureInfo.InvariantCulture, $"Asset {id}");

	public int MaxInstanceCount { get; } = maxInstanceCount ?? Unlimited;

	public IReadOnlyDictionary<string, CoreVariant> Properties { get; } =
		properties ?? FrozenDictionary<string, CoreVariant>.Empty;

	public override string ToString() =>
		$"Asset(id={Id}, name={Name}, maxInstanceCount={MaxInstanceCount}, properties={Properties})";
}
