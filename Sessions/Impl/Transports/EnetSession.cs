using System.Net;
using System.Net.Sockets;
using EnsembleRoot.Sessions.Api;
using EnsembleRoot.Sessions.Auth;
using EnsembleRoot.Sessions.Nat;
using Godot;

#pragma warning disable CA1001

namespace EnsembleRoot.Sessions.Transports;

/// <summary>
///     A multi-player session over ENet (UDP) with DTLS, and an optional UPnP port mapping while hosting.
/// </summary>
/// <remarks>
///     Closing waits briefly for queued reliable packets (e.g., a session-ended notice) to be acknowledged.
/// </remarks>
public sealed class EnetSession(SceneMultiplayer multiplayer, ISessionConfig config, string version)
	: ISession, IPortMappingSession
{
	private const int MaxClientLimit = 4095;
	private const int CloseTimeoutMs = 500;
	private const int CloseServiceTimeoutMs = 10;

#pragma warning disable CA1859
	private readonly IPeerAuthenticator _authenticator = new HandshakeAuthenticator(version, config.Password);
#pragma warning restore CA1859

	private readonly CancellationTokenSource _resolveCts = new();

	private ENetMultiplayerPeer? _peer;
	private UpnpPortMapping? _portMapping;

	public PortMappingState PortMappingState { get; private set; }
	public string ExternalAddress { get; private set; } = string.Empty;
	public string PortMappingError { get; private set; } = string.Empty;

	public event Action? PortMappingChanged;

	public SessionMode Mode => SessionMode.MultiPlayer;
	public bool IsServer => config is HostConfig;

	public ISessionConfig Config => config;

	public event Action? Started;
	public event Action<string>? Failed;

	public void StartSession()
	{
		if (config is JoinConfig join && !IPAddress.TryParse(join.Address, out _))
			_ = ResolveThenStartAsync(join);
		else
			Start((config as JoinConfig)?.Address);
	}

	public Task StopSession()
	{
#pragma warning disable MA0042
		_resolveCts.Cancel();
#pragma warning restore MA0042

		// Nothing is wired up if starting failed, and disconnecting unconnected Godot signals logs errors
		if (_peer is not { } peer)
			return Task.CompletedTask;

		multiplayer.ConnectedToServer -= OnConnectedToServer;
		multiplayer.ConnectionFailed -= OnConnectionFailed;
		multiplayer.ServerDisconnected -= OnServerDisconnected;

		_authenticator.Failed -= OnFailed;
		_authenticator.StopAuth(multiplayer);

		Close(peer);
		_peer = null;
		multiplayer.MultiplayerPeer = null;

		return ClosePortMapping();
	}

	public string GetAddress(int peerId) => GetRemote(peerId)?.GetRemoteAddress() ?? string.Empty;

	public int GetPing(int peerId) =>
		(int)(GetRemote(peerId)?.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime) ?? 0);

	// Godot would resolve the host name synchronously, freezing the main thread for up to the resolver's timeout
	private async Task ResolveThenStartAsync(JoinConfig join)
	{
		IPAddress[] addresses;

		try
		{
			addresses = await Dns.GetHostAddressesAsync(join.Address, _resolveCts.Token).ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			return;
		}
		catch (SocketException exception)
		{
			Failed?.Invoke($"Failed to resolve {join.Address}: {exception.Message}");
			return;
		}

		if (_resolveCts.IsCancellationRequested)
			return;

		if (addresses is [])
			Failed?.Invoke($"Failed to resolve {join.Address}.");
		else
			Start(addresses[0].ToString());
	}

	private void Start(string? address)
	{
		var peer = new ENetMultiplayerPeer();
		var result = config switch
		{
			HostConfig host => peer.CreateServer(host.Port, host.MaxClientCount ?? MaxClientLimit),
			JoinConfig join => peer.CreateClient(address, join.Port),
			_ => Error.InvalidParameter
		};

		if (result is Error.Ok)
			result = SetUpDtls(peer.Host, address);

		if (result is not Error.Ok)
		{
			peer.Dispose();
			Failed?.Invoke($"Failed to start session: {result}.");

			return;
		}

		_authenticator.Failed += OnFailed;
		_authenticator.StartAuth(multiplayer, IsServer);

		multiplayer.ConnectedToServer += OnConnectedToServer;
		multiplayer.ConnectionFailed += OnConnectionFailed;
		multiplayer.ServerDisconnected += OnServerDisconnected;

		_peer = peer;
		multiplayer.MultiplayerPeer = peer;

		if (!IsServer)
			return;

		OpenPortMapping();
		Started?.Invoke();
	}

	// Clients can't verify the host's self-signed certificate yet, so this stops eavesdropping and tampering only
	private Error SetUpDtls(ENetConnection host, string? address) =>
		IsServer
			? host.DtlsServerSetup(HostCertificate.ServerOptions)
			: host.DtlsClientSetup(address, TlsOptions.ClientUnsafe());

	private void OpenPortMapping()
	{
		if (config is not HostConfig { IsUpnpEnabled: true } host)
			return;

		var mapping = new UpnpPortMapping(host.Port);
		_portMapping = mapping;

		mapping.Opened += address => SetPortMapping(PortMappingState.Open, address, string.Empty);
		mapping.Failed += error => SetPortMapping(PortMappingState.Failed, string.Empty, error);

		SetPortMapping(PortMappingState.Pending, string.Empty, string.Empty);
		mapping.Open();
	}

	private Task ClosePortMapping()
	{
		if (_portMapping is not { } mapping)
			return Task.CompletedTask;

		_portMapping = null;
		SetPortMapping(PortMappingState.Disabled, string.Empty, string.Empty);

		return mapping.DisposeAsync().AsTask();
	}

	private void SetPortMapping(PortMappingState state, string externalAddress, string error)
	{
		PortMappingState = state;
		ExternalAddress = externalAddress;
		PortMappingError = error;

		PortMappingChanged?.Invoke();
	}

	private void Close(ENetMultiplayerPeer peer)
	{
		var remotes = multiplayer.GetPeers().Select(GetRemote).OfType<ENetPacketPeer>().ToArray();

		foreach (var remote in remotes)
			remote.PeerDisconnectLater();

		var deadline = Time.GetTicksMsec() + CloseTimeoutMs;

		while (
			remotes.Any(static remote => remote.GetState() is not ENetPacketPeer.PeerState.Disconnected) &&
			Time.GetTicksMsec() < deadline)
			peer.Host.Service(CloseServiceTimeoutMs);

		peer.Close();
	}

	private ENetPacketPeer? GetRemote(int peerId) =>
		_peer is { } peer &&
		(IsServer ? peerId != MultiplayerPeer.TargetPeerServer : peerId == MultiplayerPeer.TargetPeerServer)
			? peer.GetPeer(peerId)
			: null;

	private void OnConnectedToServer() => Started?.Invoke();
	private void OnConnectionFailed() => Failed?.Invoke("Connection failed.");
	private void OnServerDisconnected() => Failed?.Invoke("Disconnected from server.");
	private void OnFailed(string reason) => Failed?.Invoke(reason);
}
