namespace EnsembleRoot.Common.Networking;

public static class Http
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	public static readonly HttpClient Client;

	static Http()
	{
		var inner = new SocketsHttpHandler { ConnectTimeout = Timeout };
		Client = new HttpClient(new HttpTimeoutHandler(Timeout) { InnerHandler = inner }) { Timeout = Timeout };
	}
}
