using System.Globalization;
using Godot;
using Root.Common.Globals;
using Root.SessionManager.Api;
using Root.SessionManager.Impl;
using Serilog;

namespace Root.SessionManager.Gd;

[GlobalClass]
public partial class SessionManager : Node
{
	[Signal]
	public delegate void PeerConnectedEventHandler(int peerId);

	[Signal]
	public delegate void PeerDisconnectedEventHandler(int peerId);

	[Signal]
	public delegate void SessionFailedEventHandler(string reason);

	[Signal]
	public delegate void SessionStartedEventHandler();

	[Signal]
	public delegate void SessionStoppedEventHandler();

	private ISession? _session;

	public static SessionManager? Instance
	{
		get;
		private set
		{
			field = value;

			Log.Debug("{Class}.{Member} set (hash: {Hash})",
				nameof(SessionManager),
				nameof(Instance),
				value?.GetHashCode());
		}
	}

	public SessionMode Mode => _session?.Mode ?? SessionMode.Inactive;

	public bool IsServer => _session?.IsServer ?? false;
	public bool IsActive => _session?.IsActive ?? false;

	public double UtcStartedAtUnix => _session?.UtcStartedAt.ToUnixTimeSeconds() ?? 0;

	public int LocalPeerId { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;

		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnectedRpc;
	}

	public override void _ExitTree()
	{
		Multiplayer.PeerConnected -= OnPeerConnected;
		Multiplayer.PeerDisconnected -= OnPeerDisconnected;
		Multiplayer.PeerDisconnected -= OnPeerDisconnectedRpc;

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public void StartSinglePlayer()
	{
		Log.Debug("Starting single-player session...");

		StopSession();

		_session = new SinglePlayerSession((SceneMultiplayer)Multiplayer);
		StartSession();
	}

	public void HostMultiPlayer(int port) => HostMultiPlayer(port, string.Empty);
	public void HostMultiPlayer(int port, string password) => HostMultiPlayer(port, password, Unlimited);

	public void HostMultiPlayer(int port, string password, int maxPlayers)
	{
		Log.Debug("Hosting multiplayer session on port {Port} (max players: {MaxPlayers})",
			port,
			maxPlayers is Unlimited ? "unlimited" : maxPlayers.ToString(CultureInfo.InvariantCulture));

		StopSession();

		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new HostConfig(
				port,
				password == string.Empty ? null : password,
				maxPlayers is Unlimited ? null : maxPlayers));

		StartSession();
	}

	public void JoinMultiPlayer(string address, int port) => JoinMultiPlayer(address, port, string.Empty);

	public void JoinMultiPlayer(string address, int port, string password)
	{
		Log.Debug("Joining multiplayer session at {Address}:{Port}", address, port);

		StopSession();

		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new JoinConfig(address, port, password == string.Empty ? null : password));

		StartSession();
	}

	public void StopSession()
	{
		if (_session is null)
			return;

		var session = _session;
		_session = null;

		session.StopSession();

		session.Started -= OnSessionStarted;
		session.Stopped -= OnSessionStopped;
		session.Failed -= OnSessionFailed;
	}

	private void StartSession()
	{
		_session!.Started += OnSessionStarted;
		_session.Stopped += OnSessionStopped;
		_session.Failed += OnSessionFailed;

		_session.StartSession();
	}

	private void OnPeerConnected(long peerId)
	{
		var playerId = PlayerIdsByPeerId[(int)peerId];
		GPlayers.Add(playerId);

		if (peerId == LocalPeerId)
			GPlayers.SetLocal(playerId);

		Log.Debug("Peer connected: {PeerId} (player {PlayerId})", peerId, playerId);

		EmitSignal(SignalName.PeerConnected, (int)peerId);
	}

	private void OnPeerDisconnected(long peerId)
	{
		if (PlayerIdsByPeerId.TryGetValue((int)peerId, out var playerId))
			GPlayers.Remove(playerId);

		Log.Debug("Peer disconnected: {PeerId}", peerId);

		EmitSignal(SignalName.PeerDisconnected, (int)peerId);
	}

	private void OnSessionStarted()
	{
		LocalPeerId = Multiplayer.GetUniqueId();

		var id = LoadOrGeneratePlayerId();
		AddPeer(LocalPeerId, id);

		if (_session is { IsServer: false })
			RpcId(1, MethodName.RpcSyncPlayerAdded, id, string.Empty);

		EmitSignal(SignalName.SessionStarted);
	}

	private void OnSessionStopped()
	{
		ClearPeers();
		EmitSignal(SignalName.SessionStopped);
	}

	private void OnSessionFailed(string reason)
	{
		Log.Warning("Session failed: {Reason}", reason);
		EmitSignal(SignalName.SessionFailed, reason);
	}

	private static string LoadOrGeneratePlayerId()
	{
		var config = new ConfigFile();

#if RELEASE
		if (config.Load(CommonConstants.UserDataCfgPath) is Error.Ok)
		{
			var stored = config.GetValue("player", "id", string.Empty).AsString();

			if (Guid.TryParse(stored, out _))
				return stored;
		}
#endif

		var id = Guid.NewGuid().ToString();
		config.SetValue("player", "id", id);
		config.Save(CommonConstants.UserDataCfgPath);

		return id;
	}
}
