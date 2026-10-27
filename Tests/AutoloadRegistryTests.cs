using System.Globalization;
using EnsembleRoot.Autoloading;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class AutoloadRegistryTests
{
	// Ties would fall back to type names, so autoloads that can run together must not share an order
	[Fact]
	public void GetAll_OrdersAreUniqueWithinScopes()
	{
		var definitions = AutoloadRegistry.GetAll();
		Assert.NotEmpty(definitions);

		foreach (var definition in definitions)
			foreach (var other in definitions)
				Assert.False(
					definition.Type != other.Type && definition.Attribute.Order == other.Attribute.Order &&
					(definition.Attribute.Scope & other.Attribute.Scope) is not AutoloadScope.None,
					string.Create(
						CultureInfo.InvariantCulture,
						$"{definition.Type.Name} and {other.Type.Name} share order {definition.Attribute.Order}."));
	}
}
