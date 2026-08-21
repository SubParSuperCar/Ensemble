using Godot;
using Root.Autoloading;
using Root.SessionManager.Api;
using Root.SessionManager.Auth;
using Root.SessionManager.Impl;
using Serilog;

namespace Root.SessionManager;

[GlobalClass]
[Autoload(Order = sbyte.MinValue + 2, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class SessionManager : Node, IAutoload
{
	[Signal]
	public delegate void PeerConnectedEventHandler(int peerId);

	[Signal]
	public delegate void PeerDisconnectedEventHandler(int peerId);

	[Signal]
	public delegate void PlayerRegisteredEventHandler(int peerId, string playerId, string displayName);

	[Signal]
	public delegate void PlayerUnregisteredEventHandler(int peerId, string playerId);

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

			Log.Debug("{Class}.{Member} set. (Hash={Hash})",
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

	public void Initialize()
	{
		Instance = this;

		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
	}

	public override void _ExitTree()
	{
		Multiplayer.PeerConnected -= OnPeerConnected;
		Multiplayer.PeerDisconnected -= OnPeerDisconnected;

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public void StartSinglePlayer()
	{
		StopSession();

		_session = new SinglePlayerSession((SceneMultiplayer)Multiplayer);
		StartSession();
	}

	public void HostMultiPlayer(int port) => HostMultiPlayer(port, string.Empty);
	public void HostMultiPlayer(int port, string password) => HostMultiPlayer(port, password, -1);

	public void HostMultiPlayer(int port, string password, int maxPlayers)
	{
		StopSession();

		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new HostConfig(
				port,
				Authenticators.Password(password),
				maxPlayers is -1 ? null : maxPlayers));

		StartSession();
	}

	public void JoinMultiPlayer(string address, int port) => JoinMultiPlayer(address, port, string.Empty);

	public void JoinMultiPlayer(string address, int port, string password)
	{
		StopSession();

		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new JoinConfig(address, port, Authenticators.Password(password)));

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

	private void OnPeerConnected(long peerId) => EmitSignal(SignalName.PeerConnected, (int)peerId);

	private void OnPeerDisconnected(long peerId)
	{
		OnPeerDisconnectedDisposeRateLimiter(peerId);

		// Is register/unregister the correct term for the code base? Keep it because it's distinct?
		if (IsServer)
			BroadcastUnregister((int)peerId);

		EmitSignal(SignalName.PeerDisconnected, (int)peerId);
	}

	private void OnSessionStarted()
	{
		LocalPeerId = Multiplayer.GetUniqueId();

		// Eventually, when peers/local player are added/removed, a call to GPlayers.Add/Remove/SetLocal should be made.
		// We probably shouldn't make GPlayers or SessionManager dependant upon each other, though.
		// We might want to add a separate sync Autoload w/ Order -125 to keep peers and players in sync, but make
		// sure it runs first so that external entities comparing the two don't see sync issues.

		// The question is though: How would SessionManager be "agnostic" to GPlayers if it must provide the GUID
		// of the associated player/peer, which is a GPlayers concern? While it doesn't directly use GPlayers, that's
		// still basically being dependent in some way, but it's still better to use a sync Autoload, probably.
		// We could probably just put it under Scripts or somewhere that makes sense under a logical name. Remember
		// that it must be able to handle things first so that another resource also listening to peers doesn't see one
		// getting added and not having an associated player yet because the syncer wasn't notified yet.

		// I'd almost say that display names are unnecessary, but it's actually a good idea because it's what
		// lets users choose their names in-game. However, it should be made sure that the API actually exposes a member
		// for being able to pick your name. Right now, none of the "Join" or "Host" methods let you choose your
		// display name, so it's pretty much useless, because it'll ultimately fall back to GPlayer default (GUID).

		// Perhaps we could add optional predicate actions/methods called "OnPeerAdded" or "OnPeerRemoved" or something
		// like that that makes sense for syncing players and then pass the Peer ID and/or Player ID as necessary.
		var playerId = LoadOrCreatePlayerId();
		RegisterLocalPlayer(playerId, string.Empty);

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

		// TODO: Log actual errors w/ Log.Warning(...)
		// Also re-implement some of the original useful logging elsewhere in SessionManager with consistent messages.
#if ENSEMBLE_RELEASE
		if (config.Load(UserDataCfgPath) is Error.Ok)
		{
			var stored = config.GetValue("player", "id", string.Empty).AsString();

			if (Guid.TryParse(stored, out _))
				return stored;
		}
#endif

		var id = Guid.CreateVersion7().ToString();
		config.SetValue("player", "id", id);
		config.Save(UserDataCfgPath);

		return id;
	}
}
