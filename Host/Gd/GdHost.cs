using Godot;
using Root.Gd.Globals;
using Root.Host.Api;
using Root.Host.Impl;

namespace Root.Host.Gd;

public partial class GdHost : Node
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

	public static GdHost? Instance
	{
		get;
		private set
		{
			field = value;
			Console.WriteLine($"{nameof(GdHost)}.{nameof(Instance)} set");
		}
	}

	public SessionMode Mode => _session?.Mode ?? SessionMode.Inactive;

	public bool IsServer => _session?.IsServer ?? false;
	public bool IsActive => _session?.IsActive ?? false;

	public double UtcStartedAtUnix =>
		_session is null
			? 0
			: new DateTimeOffset(_session.UtcStartedAt).ToUnixTimeSeconds();

	public int PeerId => Multiplayer.GetUniqueId();

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
		StopSession();

		_session = new SinglePlayerSession((SceneMultiplayer)Multiplayer);
		StartSession();
	}

	public void HostMultiPlayer(int port) => HostMultiPlayer(port, string.Empty, 0);
	public void HostMultiPlayer(int port, string password) => HostMultiPlayer(port, password, 0);

	public void HostMultiPlayer(int port, string password, int maxPlayers)
	{
		StopSession();

		OS.LowProcessorUsageMode = false;

		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new HostConfig(
				port,
				password == string.Empty ? null : password,
				maxPlayers == 0 ? null : maxPlayers));

		StartSession();
	}

	public void JoinMultiPlayer(string address, int port) => JoinMultiPlayer(address, port, string.Empty);

	public void JoinMultiPlayer(string address, int port, string password)
	{
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

		OS.LowProcessorUsageMode = true;

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

	private void OnPeerConnected(long peerId) => EmitSignal(SignalName.PeerConnected, (int)peerId);
	private void OnPeerDisconnected(long peerId) => EmitSignal(SignalName.PeerDisconnected, (int)peerId);

	private void OnSessionStarted()
	{
		var id = LoadOrCreatePlayerId();
		AddPeer(PeerId, id);
		Players.SetLocal(id);

		if (_session is { IsServer: false })
			RpcId(1, MethodName.RpcSyncPlayerAdded, id, string.Empty);

		EmitSignal(SignalName.SessionStarted);
	}

	private void OnSessionStopped()
	{
		ClearPeers();
		EmitSignal(SignalName.SessionStopped);
	}

	private void OnSessionFailed(string reason) => EmitSignal(SignalName.SessionFailed, reason);

	private static string LoadOrCreatePlayerId()
	{
		var config = new ConfigFile();

		if (config.Load(Constants.UserDataCfgPath) == Error.Ok)
		{
			var stored = config.GetValue("player", "id", "").AsString();

			if (Guid.TryParse(stored, out _))
				return stored;
		}

		var id = Guid.NewGuid().ToString();
		config.SetValue("player", "id", id);
		config.Save(Constants.UserDataCfgPath);

		return id;
	}
}
