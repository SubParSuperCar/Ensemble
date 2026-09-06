using System.Diagnostics;
using System.Globalization;
using Godot;
using Root.Autoloading;
using Root.SessionManager.Api;
using Root.SessionManager.Auth;
using Root.SessionManager.Sessions;
using Serilog;

namespace Root.SessionManager;

/// <summary>
///     The <see cref="GdCore" />-agnostic session lifetime manager using Godot's <see cref="MultiplayerApi" />.
///     Provides resources for starting and stopping single and multi-player sessions,
///     handling password authentication, managing RPC actions, and more.
/// </summary>
[GlobalClass]
[Autoload(Order = sbyte.MinValue + 2, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class SessionManager : Node
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

	private string _pendingDisplayName = string.Empty;

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

	public DateTimeOffset UtcStartedAt => _session?.UtcStartedAt ?? default;
	public double UtcStartedAtUnix => UtcStartedAt.ToUnixTimeSeconds();

	public int LocalPeerId { get; private set; }

	public override void _EnterTree()
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

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressedByEvent("test_session_reset", @event))
			return;

		Log.Information("Restarting session as single-player (test action)...");
		StartSinglePlayer();
	}

	public void StartSinglePlayer() => StartSinglePlayer(string.Empty);

	public void StartSinglePlayer(string displayName)
	{
		Log.Debug("Starting {Class}...", nameof(SinglePlayerSession));

		StopSession();
		var stopwatch = Stopwatch.StartNew();

		_pendingDisplayName = displayName;
		_session = new SinglePlayerSession((SceneMultiplayer)Multiplayer);

		StartSession();

		stopwatch.Stop();
		Log.Debug("Started {Class} in {ElapsedMs:F3} ms.",
			nameof(SinglePlayerSession), stopwatch.Elapsed.TotalMilliseconds);
	}

	public void HostMultiPlayer(int port) => HostMultiPlayer(port, string.Empty);
	public void HostMultiPlayer(int port, string password) => HostMultiPlayer(port, password, Unlimited);

	public void HostMultiPlayer(int port, string? password, int maxPlayers) =>
		HostMultiPlayer(port, password, string.Empty, maxPlayers);

	public void HostMultiPlayer(int port, string? password, string displayName) =>
		HostMultiPlayer(port, password, displayName, Unlimited);

	public void HostMultiPlayer(int port, string? password, string? displayName, int maxPlayers)
	{
		Log.Debug("Hosting {Class}... (Port={Port}, MaxPlayers={MaxPlayers}, HasPassword={HasPassword})",
			nameof(MultiPlayerSession),
			port,
			maxPlayers is Unlimited ? "Unlimited" : maxPlayers.ToString(CultureInfo.InvariantCulture),
			!string.IsNullOrEmpty(password));

		StopSession();
		var stopwatch = Stopwatch.StartNew();

		_pendingDisplayName = displayName ?? string.Empty;
		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new HostConfig(
				port,
				Authenticators.Password(password),
				maxPlayers is Unlimited ? null : maxPlayers));

		StartSession();

		stopwatch.Stop();
		Log.Debug("Started {Class} (Host) in {ElapsedMs:F3} ms.",
			nameof(MultiPlayerSession), stopwatch.Elapsed.TotalMilliseconds);
	}

	public void JoinMultiPlayer(string address, int port) => JoinMultiPlayer(address, port, string.Empty);

	public void JoinMultiPlayer(string address, int port, string password) =>
		JoinMultiPlayer(address, port, password, string.Empty);

	public void JoinMultiPlayer(string address, int port, string? password, string displayName)
	{
		Log.Debug("Joining {Class}... (Address={Address}, Port={Port}, HasPassword={HasPassword})",
			nameof(MultiPlayerSession), address, port, !string.IsNullOrEmpty(password));

		StopSession();
		var stopwatch = Stopwatch.StartNew();

		_pendingDisplayName = displayName;
		_session = new MultiPlayerSession(
			(SceneMultiplayer)Multiplayer,
			new JoinConfig(address, port, Authenticators.Password(password)));

		StartSession();

		stopwatch.Stop();
		Log.Debug("Started {Class} (Join) in {ElapsedMs:F3} ms.",
			nameof(MultiPlayerSession), stopwatch.Elapsed.TotalMilliseconds);
	}

	public void StopSession()
	{
		if (_session is null)
			return;

		var mode = Mode;
		Log.Debug("Stopping {SessionMode} after {Elapsed}...", mode, GTimeProvider.GetUtcNow() - UtcStartedAt);
		var stopwatch = Stopwatch.StartNew();

		var session = _session;
		_session = null;

		session.StopSession();

		session.Started -= OnSessionStarted;
		session.Stopped -= OnSessionStopped;
		session.Failed -= OnSessionFailed;

		stopwatch.Stop();
		Log.Debug("Stopped {SessionMode} in {ElapsedMs:F3} ms.", mode, stopwatch.Elapsed.TotalMilliseconds);
	}

	private static string LoadOrGeneratePlayerId()
	{
		var config = new ConfigFile();

#if ENSEMBLE_RELEASE
		var result = config.Load(UserDataCfgPath);

		if (result is Error.Ok)
		{
			var stored = config.GetValue("player", "id", string.Empty).AsString();

			if (Guid.TryParse(stored, out _))
				return stored;

			Log.Warning("Stored player ID is not a valid GUID: {Value}", stored);
		}
		else if (result is not Error.FileNotFound)
			Log.Warning("Failed to load player data: {Error}", result);
#endif

		var id = Guid.NewGuid().ToString();

		config.SetValue("player", "id", id);
		config.Save(UserDataCfgPath);

		return id;
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
		Log.Debug("Peer connected: {PeerId}", peerId);
		EmitSignal(SignalName.PeerConnected, (int)peerId);
	}

	private void OnPeerDisconnected(long peerId)
	{
		Log.Debug("Peer disconnected: {PeerId}", peerId);

		OnPeerDisconnectedDisposeRateLimiter(peerId);

		var id = (int)peerId;
		if (IsServer)
			BroadcastUnregister(id);

		EmitSignal(SignalName.PeerDisconnected, id);
	}

	private void OnSessionStarted()
	{
		LocalPeerId = Multiplayer.GetUniqueId();

		var playerId = LoadOrGeneratePlayerId();
		RegisterLocalPlayer(playerId, _pendingDisplayName);

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
}
