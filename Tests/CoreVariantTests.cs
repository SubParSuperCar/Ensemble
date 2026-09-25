using System.Runtime.InteropServices;
using EnsembleCoreRoot.Api.Assets;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class CoreVariantTests
{
	[Fact]
	public void CoreVariant_HasExpectedSize() => Assert.Equal(24, Marshal.SizeOf<CoreVariant>());
}
