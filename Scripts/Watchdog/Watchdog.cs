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

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressedByEvent("test_hang", @event))
			return;

		Log.Warning("Hanging process (test action)...");
		Thread.Sleep(int.MaxValue);
	}

	public static void Heartbeat() => Volatile.Write(ref _heartbeatFlag, 1);

	private void WatchdogPollLoop()
	{
		try
		{
			var missCount = 0;

			while (!_cts.Token.WaitHandle.WaitOne(PollIntervalMs))
			{
				if (Debugger.IsAttached || Volatile.Read(ref _heartbeatFlag) is 1)
					missCount = 0;
				else
					OnMissed(++missCount);

				Volatile.Write(ref _heartbeatFlag, 0);
			}
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			Main.FailFast(exception);
		}
	}

	private static void OnMissed(int missCount)
	{
		Log.Warning("{Class} heartbeat missed: {Count} / {MaxCount}",
			nameof(Watchdog), missCount, TimeoutMissCountThreshold);

		if (missCount < TimeoutMissCountThreshold)
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
