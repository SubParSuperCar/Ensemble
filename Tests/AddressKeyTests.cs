using EnsembleRoot.Sessions;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class AddressKeyTests
{
	[Theory]
	[InlineData("127.0.0.1", "127.0.0.1")]
	[InlineData("::ffff:10.0.0.1", "10.0.0.1")]
	[InlineData("2001:db8::1:2:3:4", "2001:db8::/64")]
	[InlineData("2001:db8:0:0:ffff::9", "2001:db8::/64")]
	[InlineData("2001:db8::/64", "2001:db8::/64")]
	public void TryCreate_Valid_Normalizes(string address, string expected)
	{
		Assert.True(AddressKey.TryCreate(address, out var key));
		Assert.Equal(expected, key);
	}

	[Theory]
	[InlineData("")]
	[InlineData("abc")]
	[InlineData("12345")]
	[InlineData("1.2")]
	[InlineData("0.0.0.0")]
	[InlineData("::")]
	[InlineData("1.2.3.4/64")]
	[InlineData("2001:db8::/48")]
	public void TryCreate_Invalid_Fails(string address) => Assert.False(AddressKey.TryCreate(address, out _));
}
