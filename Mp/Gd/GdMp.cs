using Godot;
using Root.Globals;
using Root.Mp.Api;
using Root.Mp.Impl;

namespace Root.Mp.Gd;

public partial class GdMp : Node
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

	public static GdMp? Instance { get; private set; }

	public bool IsServer => _session?.IsServer ?? false;
	public bool IsActive => _session?.IsActive ?? false;

	public double UtcStartedAtUnix =>
		_session is null
			? 0
			: new DateTimeOffset(_session.UtcStartedAt).ToUnixTimeSeconds();

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _Ready()
	{
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
	}

	public void StartSinglePlayer()
	{
		StopSession();

		_session = new SinglePlayerSession((SceneMultiplayer)Multiplayer);
		_session.Started += OnSessionStarted;
		_session.Stopped += OnSessionStopped;
		_session.Failed += OnSessionFailed;
		_session.StartSession();
	}

	public void HostMultiPlayer(int port) => HostMultiPlayer(port, string.Empty, 0);
	public void HostMultiPlayer(int port, string password) => HostMultiPlayer(port, password, 0);

	public void HostMultiPlayer(int port, string password, int maxPlayers)
	{
		StopSession();

		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new HostConfig(
				port,
				password == string.Empty ? null : password,
				maxPlayers == 0 ? null : maxPlayers));

		_session.Started += OnSessionStarted;
		_session.Stopped += OnSessionStopped;
		_session.Failed += OnSessionFailed;
		_session.StartSession();
	}

	public void JoinMultiPlayer(string address, int port) => JoinMultiPlayer(address, port, string.Empty);

	public void JoinMultiPlayer(string address, int port, string password)
	{
		StopSession();

		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new JoinConfig(address, port, password == string.Empty ? null : password));

		_session.Started += OnSessionStarted;
		_session.Stopped += OnSessionStopped;
		_session.Failed += OnSessionFailed;
		_session.StartSession();
	}

	public void StopSession()
	{
		if (_session is null)
			return;

		var session = _session;
		_session = null;

		session.Started -= OnSessionStarted;
		session.Stopped -= OnSessionStopped;
		session.Failed -= OnSessionFailed;
		session.StopSession();
	}

	private void OnPeerConnected(long peerId) => EmitSignal(SignalName.PeerConnected, (int)peerId);
	private void OnPeerDisconnected(long peerId) => EmitSignal(SignalName.PeerDisconnected, (int)peerId);

	private void OnSessionStarted()
	{
		// TODO: Call SendGameState?
		if (_session?.Mode == SessionMode.SinglePlayer)
		{
			var id = LoadOrCreatePlayerId();
			Players.Add(id);
			Players.SetLocal(id);

			AddPeer(1, id);
		}
		else if (_session is { IsServer: false })
		{
			var id = LoadOrCreatePlayerId();
			Players.SetLocal(id);

			RpcId(1, MethodName.RpcSyncPlayerAdded, id, string.Empty);
		}

		EmitSignal(SignalName.SessionStarted);
	}

	private void OnSessionStopped() => EmitSignal(SignalName.SessionStopped);

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
