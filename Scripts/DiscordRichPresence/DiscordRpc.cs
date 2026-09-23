using DiscordRPC;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using Godot;
using Root.Autoloading;
using Serilog;

namespace Root.Scripts.DiscordRichPresence;

// TODO: Fix benign errors in AOT export builds caused by IPC named pipe socket exceptions
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
	private readonly Timestamps _timestamps = Timestamps.Now;

	private DiscordRpcClient? _client;
	private int _connectionAttemptCount;
	private int _isReconnectingFlag;

	public void Initialize()
	{
		Log.Debug("Discord RPC app ID: {AppId}", AppId);
		Connect();
	}

	public override void _ExitTree()
	{
		_cts.Cancel();

		DisposeClient();
		_cts.Dispose();
	}

	private static TimeSpan GetRetryDelay(int attemptCount) =>
		TimeSpan.FromTicks(Math.Min(FirstRetryDelay.Ticks << (attemptCount - 1), MaxRetryDelay.Ticks));

	private void Connect()
	{
		if (_cts.IsCancellationRequested)
			return;

		var client = new DiscordRpcClient(AppId)
		{
			Logger = new ConsoleLogger(LogLevel.Warning, true)
		};

		client.OnReady += OnReady;
		client.OnConnectionFailed += OnConnectionFailed;

		client.SetPresence(new RichPresence
		{
			Timestamps = _timestamps,
			Details = "By SubParSuperCar on GitHub",
			DetailsUrl = "https://github.com/SubParSuperCar/Ensemble"
		});

		_client = client;

		if (!client.Initialize())
			ScheduleReconnect();
	}

	private void DisposeClient()
	{
		if (Interlocked.Exchange(ref _client, null) is not { } client)
			return;

		Log.Debug("Terminating {$Client}...", client);

		client.OnReady -= OnReady;
		client.OnConnectionFailed -= OnConnectionFailed;

		client.Dispose();
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
				Log.Debug(
					"Gave up on Discord after {Count} connection attempt(s)",
					attemptCount);

				Callable.From(QueueFree).CallDeferred();
				return;
			}

			var delay = GetRetryDelay(attemptCount);

			Log.Verbose(
				"Connection to Discord failed. Reconnecting in {Delay:g}... (Attempt={Attempt}/{MaxAttemptCount})",
				delay,
				attemptCount + 1,
				MaxConnectionAttemptCount);

			await Task.Delay(delay, GTimeProvider, _cts.Token).ConfigureAwait(false);

			Interlocked.Exchange(ref _isReconnectingFlag, 0);
			Connect();
		}
		catch (OperationCanceledException) { }
	}
}
