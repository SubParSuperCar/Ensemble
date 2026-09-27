using EnsembleRoot.SessionManager.Api;
using Godot;

namespace EnsembleRoot.SessionManager.Sessions;

public sealed class MultiPlayerSession(SceneMultiplayer multiplayer, ISessionConfig config) : ISession
{
	private const int MaxClientLimit = 4095;
	private const int CloseTimeoutMs = 500;

	public SessionMode Mode => SessionMode.MultiPlayer;
	public bool IsServer => config is HostConfig;

	public event Action? Started;
	public event Action<string>? Failed;

	public void StartSession()
	{
		var peer = new ENetMultiplayerPeer();

		var result = config switch
		{
			HostConfig host => peer.CreateServer(host.Port, host.MaxClientCount ?? MaxClientLimit),
			JoinConfig join => peer.CreateClient(join.Address, join.Port),
			_ => Error.InvalidParameter
		};

		if (result is not Error.Ok)
		{
			Failed?.Invoke($"Failed to start session: {result}.");
			return;
		}

		config.Authenticator.Failed += OnFailed;
		config.Authenticator.StartAuth(multiplayer, IsServer);

		multiplayer.ConnectedToServer += OnConnectedToServer;
		multiplayer.ConnectionFailed += OnConnectionFailed;
		multiplayer.ServerDisconnected += OnServerDisconnected;

		multiplayer.MultiplayerPeer = peer;

		if (IsServer)
			Started?.Invoke();
	}

	public void StopSession()
	{
		multiplayer.ConnectedToServer -= OnConnectedToServer;
		multiplayer.ConnectionFailed -= OnConnectionFailed;
		multiplayer.ServerDisconnected -= OnServerDisconnected;

		config.Authenticator.Failed -= OnFailed;
		config.Authenticator.StopAuth(multiplayer);

		if (multiplayer.MultiplayerPeer is ENetMultiplayerPeer peer)
			Close(peer);

		multiplayer.MultiplayerPeer = null;
	}

	// Lets queued reliable packets (e.g., a session-ended notice) be acknowledged before disconnecting
	private void Close(ENetMultiplayerPeer peer)
	{
		var remotes = multiplayer.GetPeers().Select(peer.GetPeer).ToArray();

		foreach (var remote in remotes)
			remote.PeerDisconnectLater();

		var deadline = Time.GetTicksMsec() + CloseTimeoutMs;

		while (
			remotes.Any(static remote => remote.GetState() is not ENetPacketPeer.PeerState.Disconnected) &&
			Time.GetTicksMsec() < deadline)
			peer.Host.Service(10);

		peer.Close();
	}

	private void OnConnectedToServer() => Started?.Invoke();
	private void OnConnectionFailed() => Failed?.Invoke("Connection failed.");
	private void OnServerDisconnected() => Failed?.Invoke("Disconnected from server.");
	private void OnFailed(string reason) => Failed?.Invoke(reason);
}
