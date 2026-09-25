using System.Diagnostics;
using System.Globalization;
using EnsembleRoot.Autoloading;
using Godot;
using Serilog;
using TinyDialogsNet;

namespace EnsembleRoot.Scripts.Watchdog;

[GlobalClass]
[Autoload(Order = AutoloadOrder.Last, FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class Watchdog : Node, IAutoload
{
	private const int PollIntervalMs = (int)TimeSpan.MillisecondsPerSecond;
	private const int TimeoutMissCountThreshold = 20;

	private static byte _heartbeatFlag;

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

#if ENSEMBLE_DEBUG
	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressedByEvent("test_hang", @event))
			return;

		Log.Warning("Hanging main thread (test action)...");
		Thread.Sleep(int.MaxValue);
	}
#endif

	public static void Heartbeat() => Volatile.Write(ref _heartbeatFlag, 1);

	private static void OnMissed(int missCount)
	{
		Log.Warning(
			"{Class} heartbeat missed: {Count} / {MaxCount}",
			nameof(Watchdog),
			missCount,
			TimeoutMissCountThreshold);

		if (missCount < TimeoutMissCountThreshold)
			return;

		var elapsedMs = missCount * PollIntervalMs;
		var message = string.Create(
			CultureInfo.InvariantCulture,
			$"Main thread missed {missCount} heartbeat(s) in ~{elapsedMs} ms.");

		try
		{
			Log.Fatal("{Message}", message);
			TinyDialogs.NotifyPopup(NotificationIconType.Error, "Ensemble Watchdog Timed Out", message);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to show watchdog timeout popup");
		}
		finally
		{
			Main.FailFast();
		}
	}

	private void WatchdogPollLoop()
	{
		try
		{
			var missCount = 0;

			while (!_cts.Token.WaitHandle.WaitOne(PollIntervalMs))
			{
				if (Interlocked.Exchange(ref _heartbeatFlag, 0) is 1 || Debugger.IsAttached)
					missCount = 0;
				else
					OnMissed(++missCount);
			}
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			Main.FailFast(exception);
		}
	}
}
