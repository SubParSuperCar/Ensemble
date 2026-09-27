namespace EnsembleRoot.Common.Networking;

public sealed class HttpTimeoutHandler(TimeSpan timeout) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(timeout);

		return await base.SendAsync(request, cts.Token).ConfigureAwait(false);
	}
}
