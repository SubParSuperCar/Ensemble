namespace Root.Core.Api.Asset;

public interface IAsset
{
	int Id { get; }
	string Name { get; }

	int MaxInstanceCount { get; }

	IReadOnlyDictionary<string, Variant> Properties { get; }
}
