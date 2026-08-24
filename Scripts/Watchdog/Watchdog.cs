using System.Diagnostics;
using System.Globalization;
using Godot;
using Root.Autoloading;
using Serilog;
using TinyDialogsNet;

namespace Root.Scripts.Watchdog;

[GlobalClass]
[Autoload(Order = sbyte.MaxValue, FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class Watchdog : Node, IAutoload
{
	private const int PollIntervalMs = (int)TimeSpan.MillisecondsPerSecond;
	private const int TimeoutMissCount = 20;

	private static byte _heartbeat;

	private CancellationTokenSource _cts = null!;
	private Thread _pollThread = null!;

	public static Watchdog? Instance { get; private set; }

	public void Initialize()
	{
		Instance = this;

		Heartbeat();

		_cts = new CancellationTokenSource();

		_pollThread = new Thread(WatchdogPollLoop)
		{
			IsBackground = true,
			Name = nameof(WatchdogPollLoop)
		};
		_pollThread.Start();
	}

	public override void _ExitTree()
	{
		_cts.Cancel();
		_pollThread.Join(PollIntervalMs);

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

	public override void _Process(double delta) => Heartbeat();

	public static void Heartbeat() => Volatile.Write(ref _heartbeat, 1);

	private void WatchdogPollLoop()
	{
		try
		{
			var missCount = 0;

			while (!_cts.Token.WaitHandle.WaitOne(PollIntervalMs))
			{
				if (Debugger.IsAttached || Volatile.Read(ref _heartbeat) is 1)
					missCount = 0;
				else
					OnMissed(++missCount);

				Volatile.Write(ref _heartbeat, 0);
			}
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			Main.FailFast();
		}
	}

	private static void OnMissed(int missCount)
	{
		Log.Warning("{Class} heartbeat missed: {Count} / {MaxCount}", nameof(Watchdog), missCount, TimeoutMissCount);

		if (missCount < TimeoutMissCount)
			return;

		var elapsedMs = missCount * PollIntervalMs;
		var message = string.Create(CultureInfo.InvariantCulture,
			$"Main thread missed {missCount} heartbeat(s) in ~{elapsedMs} ms.");

		try
		{
			Log.Fatal("{Message}", message);
			Log.CloseAndFlush();

			TinyDialogs.NotifyPopup(NotificationIconType.Error, "Ensemble Watchdog Timed Out", message);
		}
		finally
		{
			Main.FailFast();
		}
	}
}
