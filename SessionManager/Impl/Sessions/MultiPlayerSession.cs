using System.Diagnostics.CodeAnalysis;
using EnsembleRoot.SessionManager.Api;
using EnsembleRoot.SessionManager.Auth;
using Godot;

namespace EnsembleRoot.SessionManager.Sessions;

/// <remarks>
///     Closing waits briefly for queued reliable packets (e.g., a session-ended notice) to be acknowledged.
/// </remarks>
public sealed class MultiPlayerSession(SceneMultiplayer multiplayer, ISessionConfig config, string version) : ISession
{
	private const int MaxClientLimit = 4095;
	private const int CloseTimeoutMs = 500;

	[SuppressMessage("Performance", "CA1859")]
	private readonly IPeerAuthenticator _authenticator = new HandshakeAuthenticator(version, config.Password);

	public SessionMode Mode => SessionMode.MultiPlayer;
	public bool IsServer => config is HostConfig;

	public ISessionConfig Config => config;

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

		_authenticator.Failed += OnFailed;
		_authenticator.StartAuth(multiplayer, IsServer);

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

		_authenticator.Failed -= OnFailed;
		_authenticator.StopAuth(multiplayer);

		if (multiplayer.MultiplayerPeer is ENetMultiplayerPeer peer)
			Close(peer);

		multiplayer.MultiplayerPeer = null;
	}

	public string GetAddress(int peerId) => GetRemote(peerId)?.GetRemoteAddress() ?? string.Empty;

	public int GetPing(int peerId) =>
		(int)(GetRemote(peerId)?.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime) ?? 0);

	private void Close(ENetMultiplayerPeer peer)
	{
		var remotes = multiplayer.GetPeers().Select(GetRemote).OfType<ENetPacketPeer>().ToArray();

		foreach (var remote in remotes)
			remote.PeerDisconnectLater();

		var deadline = Time.GetTicksMsec() + CloseTimeoutMs;
		while (
			remotes.Any(static remote => remote.GetState() is not ENetPacketPeer.PeerState.Disconnected) &&
			Time.GetTicksMsec() < deadline)
			peer.Host.Service(10);

		peer.Close();
	}

	private ENetPacketPeer? GetRemote(int peerId) =>
		multiplayer.MultiplayerPeer is ENetMultiplayerPeer peer &&
		(IsServer ? peerId != MultiplayerPeer.TargetPeerServer : peerId == MultiplayerPeer.TargetPeerServer)
			? peer.GetPeer(peerId)
			: null;

	private void OnConnectedToServer() => Started?.Invoke();
	private void OnConnectionFailed() => Failed?.Invoke("Connection failed.");
	private void OnServerDisconnected() => Failed?.Invoke("Disconnected from server.");
	private void OnFailed(string reason) => Failed?.Invoke(reason);
}
