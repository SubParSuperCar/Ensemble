using Godot;
using Root.SessionManager.Api;
using Serilog;

namespace Root.SessionManager.Sessions;

public sealed class MultiPlayerSession(SceneMultiplayer multiplayer, ISessionConfig config) : ISession
{
	public SessionMode Mode => SessionMode.MultiPlayer;

	public bool IsServer => multiplayer.IsServer();
	public bool IsActive { get; private set; }

	public DateTimeOffset UtcStartedAt { get; } = GTimeProvider.GetUtcNow();

	public event Action? Started;
	public event Action? Stopped;

	public event Action<string>? Failed;

	public void StartSession()
	{
		if (IsActive)
			return;

		var peer = new ENetMultiplayerPeer();

		var result = config switch
		{
			HostConfig host => host.MaxPlayerCount is null
				? peer.CreateServer(host.Port)
				: peer.CreateServer(host.Port, host.MaxPlayerCount.Value),
			JoinConfig join => peer.CreateClient(join.Address, join.Port),
			_ => Error.InvalidParameter
		};

		if (result is not Error.Ok)
		{
			Failed?.Invoke($"Failed to start session: {result}");
			return;
		}

		if (config.Authenticator is { } authenticator)
		{
			authenticator.AuthenticationFailed += OnAuthenticationFailed;
			authenticator.StartAuth(multiplayer, config is HostConfig);
		}

		multiplayer.MultiplayerPeer = peer;

		if (config is HostConfig)
		{
			IsActive = true;
			Started?.Invoke();
		}
		else
		{
			multiplayer.ConnectedToServer += OnConnectedToServer;
			multiplayer.ConnectionFailed += OnConnectionFailed;
			multiplayer.ServerDisconnected += OnServerDisconnected;
		}
	}

	public void StopSession()
	{
		if (config is JoinConfig)
		{
			multiplayer.ConnectedToServer -= OnConnectedToServer;
			multiplayer.ConnectionFailed -= OnConnectionFailed;
			multiplayer.ServerDisconnected -= OnServerDisconnected;
		}

		if (config.Authenticator is { } authenticator)
		{
			authenticator.AuthenticationFailed -= OnAuthenticationFailed;
			authenticator.StopAuth(multiplayer);
		}

		ClosePeer();

		if (!IsActive)
			return;

		IsActive = false;
		Stopped?.Invoke();
	}

	private void ClosePeer()
	{
		multiplayer.MultiplayerPeer?.Close();
		multiplayer.MultiplayerPeer = null;
	}

	private void OnConnectedToServer()
	{
		multiplayer.ConnectedToServer -= OnConnectedToServer;
		multiplayer.ConnectionFailed -= OnConnectionFailed;

		IsActive = true;
		Started?.Invoke();
	}

	private void OnConnectionFailed()
	{
		multiplayer.ConnectedToServer -= OnConnectedToServer;
		multiplayer.ConnectionFailed -= OnConnectionFailed;
		multiplayer.ServerDisconnected -= OnServerDisconnected;

		ClosePeer();

		Failed?.Invoke("Connection failed.");
	}

	private void OnServerDisconnected()
	{
		multiplayer.ServerDisconnected -= OnServerDisconnected;

		ClosePeer();

		IsActive = false;
		Stopped?.Invoke();
	}

	private void OnAuthenticationFailed(long peerId, string reason)
	{
		if (config is HostConfig)
		{
			Log.Warning("Peer {PeerId} rejected during authentication: {Reason}", peerId, reason);
			return;
		}

		Log.Warning("Authentication with server failed: {Reason}", reason);

		StopSession();
		Failed?.Invoke(reason);
	}
}
