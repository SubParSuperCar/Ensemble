namespace EnsembleRoot.Common.Networking;

public static class Http
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	public static readonly HttpClient Client =
		new(new SocketsHttpHandler { ConnectTimeout = Timeout }) { Timeout = Timeout };
}
