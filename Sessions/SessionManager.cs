using System.Diagnostics;
using System.Globalization;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Sessions.Api;
using EnsembleRoot.Sessions.Transports;
using Godot;
using Serilog;

namespace EnsembleRoot.Sessions;

/// <summary>
///     The game-agnostic session lifetime manager built on Godot's <see cref="MultiplayerApi" />. Starts and stops
///     sessions over pluggable <see cref="ISession" /> transports, authenticates peers, registers them as
///     <see cref="Peer" /> objects with server-assigned player IDs, and runs network actions and snapshots.
/// </summary>
/// <remarks>
///     A session is only started (<see cref="IsActive" />) once the local player has been registered. Clients then
///     request snapshots, and confirmed actions are only sent to peers that received them, so none can precede or
///     duplicate a snapshot. Failures are handled deferred, so a session never ends mid-poll (e.g., from an RPC).
/// </remarks>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Early + 1, FailurePolicy = AutoloadFailurePolicy.FailFast)]
public partial class SessionManager : Node
{
	[Signal]
	public delegate void ActionRejectedEventHandler(string actionId, string reason);

	[Signal]
	public delegate void PeerRegisteredEventHandler(Peer peer);

	[Signal]
	public delegate void PeerUnregisteredEventHandler(Peer peer);

	[Signal]
	public delegate void SessionFailedEventHandler(string reason);

	[Signal]
	public delegate void SessionStartedEventHandler();

	[Signal]
	public delegate void SessionStoppedEventHandler();

	public const int DefaultPort = 7777;

	private const int Unlimited = -1;

	private static readonly StringName ResetAction = "test_session_reset";

	private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(10);
	private static readonly TimeSpan KickTimeout = TimeSpan.FromSeconds(2);
	private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(1);

	private string _displayName = string.Empty;
	private ISession? _session;
	private Task _stopping = Task.CompletedTask;

	public static SessionManager? Instance
	{
		get;
		private set
		{
			field = value;
			Log.Debug(
				"{Class}.{Member} set (Hash={Hash})", nameof(SessionManager), nameof(Instance), value?.GetHashCode());
		}
	}

	public SessionMode Mode => _session?.Mode ?? SessionMode.Inactive;

	public bool IsServer => _session?.IsServer ?? false;
	public bool IsActive { get; private set; }

	public DateTimeOffset UtcStartedAt { get; private set; }
	public double UtcStartedAtUnix => UtcStartedAt.ToUnixTimeMilliseconds() / 1000d;

	public int LocalPeerId { get; private set; }

	public ISessionConfig? Config => _session?.Config;

	public int Port => Config?.Port ?? 0;
	public bool HasPassword => !string.IsNullOrEmpty(Config?.Password);
	public bool IsDedicated => Config is HostConfig { IsDedicated: true };

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
		if (what != NotificationWMCloseRequest)
			return;

		StopSession();
		_stopping.Wait(StopTimeout);
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressedByEvent(ResetAction, @event) || Mode is SessionMode.MultiPlayer)
			return;

		Log.Information("Restarting session as single-player (test action)...");
		StartSinglePlayer();
	}

	public void StartSinglePlayer() => StartSinglePlayer(string.Empty);

	public void StartSinglePlayer(string? displayName)
	{
		Log.Debug("Starting {Class}...", nameof(OfflineSession));
		StartSession(new OfflineSession((SceneMultiplayer)Multiplayer), displayName);
	}

	public void HostMultiPlayer(int port) => HostMultiPlayer(port, string.Empty);
	public void HostMultiPlayer(int port, string? password) => HostMultiPlayer(port, password, string.Empty);

	public void HostMultiPlayer(int port, string? password, string? displayName) =>
		HostMultiPlayer(port, password, displayName, Unlimited);

	public void HostMultiPlayer(int port, string? password, string? displayName, int maxClients) =>
		HostMultiPlayer(port, password, displayName, maxClients, false);

	public void HostMultiPlayer(int port, string? password, string? displayName, int maxClients, bool isDedicated) =>
		HostMultiPlayer(port, password, displayName, maxClients, isDedicated, false);

	public void HostMultiPlayer(
		int port, string? password, string? displayName, int maxClients, bool isDedicated, bool isUpnpEnabled)
	{
		Log.Debug(
			"Hosting {Class}... (Port={Port}, MaxClients={MaxClients}, HasPassword={HasPassword}, " +
			"IsDedicated={IsDedicated}, IsUpnpEnabled={IsUpnpEnabled})",
			nameof(EnetSession), port,
			maxClients is Unlimited ? "Unlimited" : maxClients.ToString(CultureInfo.InvariantCulture),
			!string.IsNullOrEmpty(password), isDedicated, isUpnpEnabled);

		var config = new HostConfig(
			port, password, maxClients is Unlimited ? null : maxClients, isDedicated, isUpnpEnabled);

		StartSession(new EnetSession((SceneMultiplayer)Multiplayer, config, GameVersion), displayName);
	}

	public void JoinMultiPlayer(string address, int port) => JoinMultiPlayer(address, port, string.Empty);

	public void JoinMultiPlayer(string address, int port, string? password) =>
		JoinMultiPlayer(address, port, password, string.Empty);

	public void JoinMultiPlayer(string address, int port, string? password, string? displayName)
	{
		Log.Debug(
			"Joining {Class}... (Address={Address}, Port={Port}, HasPassword={HasPassword})",
			nameof(EnetSession), address, port, !string.IsNullOrEmpty(password));

		var config = new JoinConfig(address, port, password);
		StartSession(new EnetSession((SceneMultiplayer)Multiplayer, config, GameVersion), displayName);
	}

	public void StartSession(ISession session) => StartSession(session, string.Empty);

	/// <summary>Stops the current session, if any, and starts <paramref name="session" />.</summary>
	/// <remarks>The entry point for custom transports; the methods above build the built-in ones.</remarks>
	public void StartSession(ISession session, string? displayName)
	{
		StopSession();
		var stopwatch = Stopwatch.StartNew();

		_session = session;
		_displayName = displayName ?? string.Empty;

		session.Started += OnSessionStarted;
		session.Failed += OnSessionFailed;
		AttachPortMapping(session);

		session.StartSession();
		stopwatch.Stop();

		if (ReferenceEquals(_session, session))
			Log.Debug(
				"Started {Class} in {ElapsedMs:F3} ms", session.GetType().Name, stopwatch.Elapsed.TotalMilliseconds);
	}

	public void StopSession() => EndSession(null);

	/// <summary>Ends the session as failed, with a reason shown to the user (and to clients, when hosting).</summary>
	/// <remarks>Deferred, so it is safe to call from session signal handlers.</remarks>
	public void FailSession(string reason) => OnSessionFailed(reason);

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
		DetachPortMapping(session);

		_stopping = Task.WhenAll(_stopping, ObserveStopAsync(session.StopSession()));

		// Inactive first, so the peers' teardown doesn't look like players leaving
		var wasActive = IsActive;
		IsActive = false;

		ClearPeers();
		ClearRpcState();

		UtcStartedAt = default;
		LocalPeerId = 0;

		stopwatch.Stop();
		Log.Debug("Stopped {SessionMode} in {ElapsedMs:F3} ms", mode, stopwatch.Elapsed.TotalMilliseconds);

		if (wasActive)
			EmitSignal(SignalName.SessionStopped);

		if (failureReason is not null)
			EmitSignal(SignalName.SessionFailed, failureReason);
	}

	// Keeps every session's cleanup awaitable at quit, and a failed one from breaking the wait
	private static async Task ObserveStopAsync(Task stopping)
	{
		try
		{
			await stopping.ConfigureAwait(false);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to clean up a stopped session");
		}
	}

	private void Activate()
	{
		IsActive = true;
		UtcStartedAt = GTimeProvider.GetUtcNow();

		EmitSignal(SignalName.SessionStarted);

		if (!IsServer)
			RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcRequestSnapshots);
	}

	private void OnSessionStarted()
	{
		LocalPeerId = Multiplayer.GetUniqueId();

		if (IsDedicated)
		{
			Activate();
			return;
		}

		if (IsServer)
		{
			RegisterPeer(LocalPeerId, _displayName);
			return;
		}

		RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RpcRequestRegister, _displayName);

		var session = _session;

		GetTree().CreateTimer(RegistrationTimeout.TotalSeconds).Timeout += () =>
		{
			if (ReferenceEquals(_session, session) && !IsActive)
				OnSessionFailed("Registration timed out.");
		};
	}

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
