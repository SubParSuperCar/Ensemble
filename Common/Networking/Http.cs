namespace EnsembleRoot.Common.Networking;

public static class Http
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	public static readonly HttpClient Client;

	static Http()
	{
		var timeoutHandler = new TimeoutHandler(Timeout)
		{
			InnerHandler = new SocketsHttpHandler
			{
				ConnectTimeout = Timeout
			}
		};

		Client = new HttpClient(timeoutHandler)
		{
			Timeout = Timeout
		};
	}
}
