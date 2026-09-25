using EnsembleRoot.Common.Networking;
using Xunit;

namespace EnsembleRoot.Tests;

public sealed class TimeoutHandlerTests
{
	[Fact]
	public async Task SendAsync_TimesOut()
	{
		var handler = new TimeoutHandler(TimeSpan.FromMilliseconds(100))
		{
			InnerHandler = new HangingHandler()
		};

		// ReSharper disable once ShortLivedHttpClient
		using var client = new HttpClient(handler);

		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			client.GetAsync("http://example.com", TestContext.Current.CancellationToken));
	}

	private sealed class HangingHandler : HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
			return null!;
		}
	}
}
