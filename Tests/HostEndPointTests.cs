using EnsembleRoot.Common.Networking;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class HostEndPointTests
{
	[Theory]
	[InlineData("192.168.1.2:7777", "192.168.1.2", 7777)]
	[InlineData(" 127.0.0.1:1024 ", "127.0.0.1", 1024)]
	[InlineData("[::1]:7777", "::1", 7777)]
	[InlineData("example.com:65535", "example.com", 65535)]
	public void TryParse_Valid_Succeeds(string value, string host, int port)
	{
		Assert.True(HostEndPoint.TryParse(value, out var endPoint));
		Assert.Equal(new HostEndPoint(host, port), endPoint);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("192.168.1.2")]
	[InlineData("192.168.1.2:0")]
	[InlineData("192.168.1.2:65536")]
	[InlineData("example.com:7777/path")]
	[InlineData("user@example.com:7777")]
	[InlineData("::1:7777")]
	public void TryParse_Invalid_Fails(string? value) => Assert.False(HostEndPoint.TryParse(value, out _));
}
