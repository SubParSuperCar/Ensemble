using System.Runtime.InteropServices;
using EnsembleCoreRoot.Api.Assets;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class CoreVariantTests
{
	[Fact]
	public void CoreVariant_HasExpectedSize() => Assert.Equal(24, Marshal.SizeOf<CoreVariant>());

	[Fact]
	public void Equals_ComparesTypeAndValue()
	{
		Assert.Equal(new CoreVariant(1), new CoreVariant(1L));
		Assert.Equal(new CoreVariant(double.NaN), new CoreVariant(double.NaN));
		Assert.Equal(CoreVariant.Null, new CoreVariant(null));

		Assert.NotEqual(new CoreVariant(1), new CoreVariant(1d));
		Assert.NotEqual(new CoreVariant(true), new CoreVariant(1));
		Assert.NotEqual(new CoreVariant("a"), new CoreVariant("A"));
	}

	[Fact]
	public void Conversions_WidenNumbersAndRejectMismatches()
	{
		Assert.Equal(2d, (double)new CoreVariant(2));
		Assert.Equal(2L, (long)new CoreVariant(2.75));

		Assert.Throws<InvalidCastException>(static () => (bool)new CoreVariant(1));
		Assert.Throws<InvalidCastException>(static () => (string)new CoreVariant(1));
	}
}
