using System.Globalization;
using EnsembleRoot.Autoloading;
using EnsembleRoot.SessionManager.Api;
using EnsembleRoot.SessionManager.Auth;
using EnsembleRoot.SessionManager.Sessions;
using Godot;
using Serilog;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace EnsembleRoot.SessionManager;

/// <summary>
///     The <see cref="GdCore" />-agnostic session lifetime manager using Godot's <see cref="MultiplayerApi" />.
///     Provides resources for starting and stopping single- and multiplayer sessions,
///     handling password authentication, registering server-assigned player IDs, managing RPC actions, and more.
///     A session is only started (<see cref="IsActive" />) once the local player has been registered.
/// </summary>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Early + 1, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class SessionManager : Node
{
	[Signal]
	public delegate void ActionRejectedEventHandler(string actionId, string reason);

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

	public const int DefaultPort = 7777;

	private const int Unlimited = -1;

	private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(10);
	private static readonly TimeSpan KickTimeout = TimeSpan.FromSeconds(2);

	private static readonly string Version = ProjectSettings.GetSetting("application/config/version").AsString();

	private string _displayName = string.Empty;
	private bool _isDedicated;

	private ISession? _session;

	public static SessionManager? Instance
	{
		get;
		private set
		{
			field = value;

			Log.Debug(
				"{Class}.{Member} set (Hash={Hash})",
				nameof(SessionManager),
				nameof(Instance),
				value?.GetHashCode());
		}
	}

	public SessionMode Mode => _session?.Mode ?? SessionMode.Inactive;

	public bool IsServer => _session?.IsServer ?? false;
	public bool IsActive { get; private set; }

	public DateTimeOffset UtcStartedAt { get; private set; }
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

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
			StopSession();
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressedByEvent("test_session_reset", @event))
			return;

		Log.Information("Restarting session as single-player (test action)...");
		StartSinglePlayer();
	}

	public void StartSinglePlayer() => StartSinglePlayer(string.Empty);

	public void StartSinglePlayer(string? displayName)
	{
		Log.Debug("Starting {Class}...", nameof(SinglePlayerSession));
		Start(new SinglePlayerSession((SceneMultiplayer)Multiplayer), displayName, false);
	}

	public void HostMultiPlayer(int port) => HostMultiPlayer(port, string.Empty);
	public void HostMultiPlayer(int port, string? password) => HostMultiPlayer(port, password, string.Empty);

	public void HostMultiPlayer(int port, string? password, string? displayName) =>
		HostMultiPlayer(port, password, displayName, Unlimited);

	public void HostMultiPlayer(int port, string? password, string? displayName, int maxClients) =>
		HostMultiPlayer(port, password, displayName, maxClients, false);

	public void HostMultiPlayer(int port, string? password, string? displayName, int maxClients, bool isDedicated)
	{
		Log.Debug(
			"Hosting {Class}... " +
			"(Port={Port}, MaxClients={MaxClients}, HasPassword={HasPassword}, IsDedicated={IsDedicated})",
			nameof(MultiPlayerSession),
			port,
			maxClients is Unlimited ? "Unlimited" : maxClients.ToString(CultureInfo.InvariantCulture),
			!string.IsNullOrEmpty(password),
			isDedicated);

		Start(
			new MultiPlayerSession(
				(SceneMultiplayer)Multiplayer,
				new HostConfig(
					port,
					new HandshakeAuthenticator(Version, password),
					maxClients is Unlimited ? null : maxClients)),
			displayName,
			isDedicated);
	}

	public void JoinMultiPlayer(string address, int port) => JoinMultiPlayer(address, port, string.Empty);

	public void JoinMultiPlayer(string address, int port, string? password) =>
		JoinMultiPlayer(address, port, password, string.Empty);

	public void JoinMultiPlayer(string address, int port, string? password, string? displayName)
	{
		Log.Debug(
			"Joining {Class}... (Address={Address}, Port={Port}, HasPassword={HasPassword})",
			nameof(MultiPlayerSession),
			address,
			port,
			!string.IsNullOrEmpty(password));

		Start(
			new MultiPlayerSession(
				(SceneMultiplayer)Multiplayer,
				new JoinConfig(address, port, new HandshakeAuthenticator(Version, password))),
			displayName,
			false);
	}

	public void StopSession() => EndSession(null);

	public void Kick(int peerId) => Kick(peerId, string.Empty);

	public void Kick(int peerId, string reason)
	{
		if (!IsServer || peerId == LocalPeerId || !Multiplayer.GetPeers().Contains(peerId))
		{
			Log.Warning("Cannot kick peer {PeerId}", peerId);
			return;
		}

		Log.Information("Kicking peer {PeerId}... (Reason={Reason})", peerId, reason);
		RpcId(peerId, MethodName.RpcEndSession, reason.Length is 0 ? "Kicked by host." : $"Kicked by host: {reason}");

		var session = _session;
		GetTree().CreateTimer(KickTimeout.TotalSeconds).Timeout += () =>
		{
			if (ReferenceEquals(_session, session) && Multiplayer.GetPeers().Contains(peerId))
				((SceneMultiplayer)Multiplayer).DisconnectPeer(peerId);
		};
	}

	[Rpc]
	private void RpcEndSession(string reason) => OnSessionFailed(reason);

	private void Start(ISession session, string? displayName, bool isDedicated)
	{
		StopSession();
		var stopwatch = Stopwatch.StartNew();

		_session = session;
		_displayName = displayName ?? string.Empty;
		_isDedicated = isDedicated;

		session.Started += OnSessionStarted;
		session.Failed += OnSessionFailed;
		session.StartSession();

		stopwatch.Stop();

		if (ReferenceEquals(_session, session))
			Log.Debug(
				"Started {Class} in {ElapsedMs:F3} ms",
				session.GetType().Name,
				stopwatch.Elapsed.TotalMilliseconds);
	}

	private void EndSession(string? failureReason)
	{
		if (_session is not { } session)
			return;

		var mode = Mode;
		var elapsed = IsActive ? GTimeProvider.GetUtcNow() - UtcStartedAt : TimeSpan.Zero;

		Log.Debug("Stopping {SessionMode} after {Elapsed}...", mode, elapsed);
		var stopwatch = Stopwatch.StartNew();

		if (IsServer && Multiplayer.GetPeers() is not [])
			Rpc(MethodName.RpcEndSession, failureReason ?? "Host ended the session.");

		_session = null;

		session.Started -= OnSessionStarted;
		session.Failed -= OnSessionFailed;
		session.StopSession();

		ClearPeers();
		ClearRpcState();

		var wasActive = IsActive;

		IsActive = false;
		UtcStartedAt = default;
		LocalPeerId = 0;

		stopwatch.Stop();
		Log.Debug("Stopped {SessionMode} in {ElapsedMs:F3} ms", mode, stopwatch.Elapsed.TotalMilliseconds);

		if (wasActive)
			EmitSignal(SignalName.SessionStopped);

		if (failureReason is not null)
			EmitSignal(SignalName.SessionFailed, failureReason);
	}

	private void Activate()
	{
		IsActive = true;
		UtcStartedAt = GTimeProvider.GetUtcNow();

		EmitSignal(SignalName.SessionStarted);
	}

	private void OnSessionStarted()
	{
		LocalPeerId = Multiplayer.GetUniqueId();

		if (_isDedicated)
		{
			Activate();
			return;
		}

		if (IsServer)
		{
			RegisterPeer(LocalPeerId, _displayName);
			return;
		}

		RpcId(ServerPeerId, MethodName.RpcRequestRegister, _displayName);

		var session = _session;
		GetTree().CreateTimer(RegistrationTimeout.TotalSeconds).Timeout += () =>
		{
			if (ReferenceEquals(_session, session) && !IsActive)
				OnSessionFailed("Registration timed out.");
		};
	}

	// Deferred so the session never ends mid-poll (e.g., from an RPC or a multiplayer signal)
	private void OnSessionFailed(string reason)
	{
		var session = _session;

		Callable.From(() =>
		{
			if (!ReferenceEquals(_session, session))
				return;

			Log.Warning("Session failed: {Reason}", reason);
			EndSession(reason);
		}).CallDeferred();
	}
}
