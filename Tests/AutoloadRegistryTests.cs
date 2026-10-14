using System.Reflection;
using EnsembleRoot.Autoloading;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class AutoloadRegistryTests
{
	// The generator writes its own defaults for omitted arguments, so they must match the attribute's
	[Fact]
	public void GetAll_MatchesAttributes()
	{
		var definitions = AutoloadRegistry.GetAll();
		Assert.NotEmpty(definitions);

		foreach (var definition in definitions)
		{
			var attribute = definition.Type.GetCustomAttribute<AutoloadAttribute>();

			Assert.NotNull(attribute);
			Assert.Equal(attribute.Scope, definition.Scope);
			Assert.Equal(attribute.Order, definition.Order);
			Assert.Equal(attribute.FailurePolicy, definition.FailurePolicy);
		}
	}
}
