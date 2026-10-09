using System.Diagnostics;
using System.Globalization;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Utils;
using Godot;
using Serilog;
using TinyDialogsNet;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Watchdog;

/// <summary>
///     Fails fast when the main thread stops sending heartbeats for too long, i.e., when it hangs.
/// </summary>
/// <remarks>
///     Heartbeats come from <see cref="_Process" />, which keeps running while the scene tree is paused. To block the
///     main thread on purpose, hold <see cref="Suspension" /> for the duration. An attached debugger also suspends it.
/// </remarks>
[GlobalClass]
[Autoload(Order = AutoloadOrder.Last, FailurePolicy = AutoloadFailurePolicy.AskUser)]
public partial class Watchdog : Node, IAutoload
{
	private const int PollIntervalMs = (int)TimeSpan.MillisecondsPerSecond;
	private const int TimeoutMissCount = 20;

#if ENSEMBLE_DEBUG
	private static readonly StringName HangAction = "test_hang";
#endif

	private static byte _heartbeatFlag;

	private readonly CancellationTokenSource _cts = new();
	private Thread? _pollThread;

	public static Watchdog? Instance { get; private set; }

	/// <summary>Suspends hang detection while held by any owner. Thread-safe.</summary>
	public static OwnershipFlag Suspension { get; } = new();

	public void Initialize()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		Heartbeat();

		_pollThread = new Thread(Poll) { IsBackground = true, Name = $"{nameof(Watchdog)}.{nameof(Poll)}" };
		_pollThread.Start();
	}

	public override void _ExitTree()
	{
		_cts.Cancel();
		_pollThread?.Join(PollIntervalMs);

		if (ReferenceEquals(Instance, this))
			Instance = null;
	}

#if ENSEMBLE_DEBUG
	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (!Input.IsActionJustPressedByEvent(HangAction, @event))
			return;

		Log.Warning("Hanging main thread (test action)...");
		Thread.Sleep(Timeout.Infinite);
	}
#endif

	public override void _Process(double delta) => Heartbeat();

	public static void Heartbeat() => Volatile.Write(ref _heartbeatFlag, 1);

	private static void OnMissed(int missCount)
	{
		Log.Warning("{Class} heartbeat missed: {Count} / {MaxCount}", nameof(Watchdog), missCount, TimeoutMissCount);

		if (missCount < TimeoutMissCount)
			return;

		var message = string.Create(
			CultureInfo.InvariantCulture,
			$"Main thread missed {missCount} heartbeat(s) in ~{missCount * PollIntervalMs} ms.");

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

	private void Poll()
	{
		try
		{
			var missCount = 0;

			while (!_cts.Token.WaitHandle.WaitOne(PollIntervalMs))
			{
				if (Interlocked.Exchange(ref _heartbeatFlag, 0) is 1 || Suspension.IsSet || Debugger.IsAttached)
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
