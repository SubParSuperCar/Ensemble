using DiscordRPC;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Sessions.Api;
using Godot;
using Serilog;

namespace EnsembleRoot.Scripts.DiscordRichPresence;

// TODO: Fix benign errors in AOT export builds caused by IPC named pipe socket exceptions
/// <remarks>
///     Shares only the activity (menu, single-player, hosting, or joined) and its elapsed time; never names,
///     addresses, ports, or player counts.
/// </remarks>
[GlobalClass]
[Autoload(
	Scope = AutoloadScope.RegularClient,
	Order = AutoloadOrder.Standard + 1,
	FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class DiscordRpc : Node, IAutoload
{
	private const string AppId = "1534319171079504002";
	private const int MaxConnectionAttemptCount = 8;

	private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(2.5);
	private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(2);

	private readonly CancellationTokenSource _cts = new();
	private readonly Timestamps _launchedAt = Timestamps.Now;

	// Guards the client and presence: reconnects run on the thread pool, presence updates on the main thread
	private readonly Lock _lock = new();

	private DiscordRpcClient? _client;
	private int _connectionAttemptCount;
	private int _isReconnectingFlag;
	private RichPresence _presence = new();

	public void Initialize()
	{
		Log.Debug("Discord RPC app ID: {AppId}", AppId);

		UpdatePresence();
		GSessionManager.SessionStarted += UpdatePresence;
		GSessionManager.SessionStopped += UpdatePresence;
		GSessionManager.SessionFailed += OnSessionFailed;

		Connect();
	}

	public override void _ExitTree()
	{
		GSessionManager.SessionStarted -= UpdatePresence;
		GSessionManager.SessionStopped -= UpdatePresence;
		GSessionManager.SessionFailed -= OnSessionFailed;

		_cts.Cancel();
		DisposeClient();
	}

	private static TimeSpan GetRetryDelay(int attemptCount) =>
		TimeSpan.FromTicks(Math.Min(FirstRetryDelay.Ticks << (attemptCount - 1), MaxRetryDelay.Ticks));

	private static string GetActivity() =>
		GSessionManager switch
		{
			{ IsActive: false } => "In the Main Menu",
			{ Mode: SessionMode.SinglePlayer } => "Playing Single-Player",
			{ Mode: SessionMode.MultiPlayer, IsServer: true } => "Hosting a Multi-Player Session",
			_ => "Playing Multi-Player"
		};

	private void Connect()
	{
		bool isInitialized;

		lock (_lock)
		{
			// Checked under the lock, so a client created during shutdown is never left undisposed
			if (_cts.IsCancellationRequested)
				return;

			var client = new DiscordRpcClient(AppId) { Logger = new ConsoleLogger(LogLevel.Error, true) };

			client.OnReady += OnReady;
			client.OnConnectionFailed += OnConnectionFailed;

			client.SetPresence(_presence);
			_client = client;

			isInitialized = client.Initialize();
		}

		if (!isInitialized)
			ScheduleReconnect();
	}

	private void DisposeClient()
	{
		DiscordRpcClient? client;

		lock (_lock)
		{
			client = _client;
			_client = null;
		}

		if (client is null)
			return;

		Log.Debug("Terminating {$Client}...", client);

		client.OnReady -= OnReady;
		client.OnConnectionFailed -= OnConnectionFailed;

		client.Dispose();
	}

	private void OnSessionFailed(string _) => UpdatePresence();

	private void UpdatePresence()
	{
		var manager = GSessionManager;

		var presence = new RichPresence
		{
			Details = "By SubParSuperCar on GitHub",
			DetailsUrl = GitHubRepoUrl,
			State = GetActivity(),
			Timestamps = manager.IsActive ? new Timestamps(manager.UtcStartedAt.UtcDateTime) : _launchedAt
		};

		lock (_lock)
		{
			_presence = presence;
			_client?.SetPresence(presence);
		}

		Log.Debug("Updated Discord presence: {State}", presence.State);
	}

	private void OnReady(object? sender, ReadyMessage e)
	{
		Volatile.Write(ref _connectionAttemptCount, 0);
		Log.Debug("Connected to Discord with user: {UserName} ({SnowflakeId})", e.User.Username, e.User.ID);
	}

	private void OnConnectionFailed(object? sender, ConnectionFailedMessage e) => ScheduleReconnect();

	private void ScheduleReconnect()
	{
		if (Interlocked.Exchange(ref _isReconnectingFlag, 1) is 0)
			_ = ReconnectAsync();
	}

	private async Task ReconnectAsync()
	{
		try
		{
			DisposeClient();

			var attemptCount = Interlocked.Increment(ref _connectionAttemptCount);

			if (attemptCount >= MaxConnectionAttemptCount)
			{
				Log.Debug("Gave up on Discord after {Count} connection attempt(s)", attemptCount);

				Callable.From(QueueFree).CallDeferred();
				return;
			}

			var delay = GetRetryDelay(attemptCount);

			Log.Verbose(
				"Connection to Discord failed. Reconnecting in {Delay:g}... (Attempt={Attempt}/{MaxAttemptCount})",
				delay, attemptCount + 1, MaxConnectionAttemptCount);

			await Task.Delay(delay, GTimeProvider, _cts.Token).ConfigureAwait(false);

			Interlocked.Exchange(ref _isReconnectingFlag, 0);
			Connect();
		}
		catch (OperationCanceledException) { }
	}
}
