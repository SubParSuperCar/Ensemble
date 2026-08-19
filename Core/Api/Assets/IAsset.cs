namespace CoreRoot.Api.Assets;

public interface IAsset
{
	int Id { get; }
	string Name { get; }

	int MaxInstanceCount { get; }

	IReadOnlyDictionary<string, CoreVariant> Properties { get; }
}
