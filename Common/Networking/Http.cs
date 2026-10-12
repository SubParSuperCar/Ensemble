namespace EnsembleRoot.Common.Networking;

public static class Http
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	// The client timeout bounds each request end to end (DNS, connect, TLS, and response), so a dead uplink can't hang
	public static readonly HttpClient Client =
		new(new SocketsHttpHandler { ConnectTimeout = Timeout }) { Timeout = Timeout };
}
