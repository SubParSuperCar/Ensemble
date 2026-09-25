using System.Runtime.InteropServices;
using CoreRoot.Api.Assets;
using Xunit;

namespace Root.Tests;

public sealed class CoreVariantTests
{
	[Fact]
	public void CoreVariant_HasExpectedSize() => Assert.Equal(24, Marshal.SizeOf<CoreVariant>());
}
