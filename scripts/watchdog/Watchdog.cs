using System.Diagnostics;
using System.Globalization;
using Godot;
using Serilog;
using Environment = System.Environment;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Watchdog;

public partial class Watchdog : Node
{
	private const int PollIntervalMs = (int)TimeSpan.MillisecondsPerSecond;
	private const int TimeoutMissCount = 15;

	private static byte _heartbeat;

	private CancellationTokenSource _cts = null!;
	private Thread _pollThread = null!;

	public static Watchdog? Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;

		Heartbeat();

		_cts = new CancellationTokenSource();

		_pollThread = new Thread(PollLoop)
		{
			IsBackground = true,
			Name = nameof(Watchdog)
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
		if (Input.IsActionJustPressedByEvent("test_hang", @event))
			Thread.Sleep(int.MaxValue);
	}

	public static void Heartbeat() => Volatile.Write(ref _heartbeat, 1);

	private void PollLoop()
	{
		try
		{
			var missCount = 0;

			while (!_cts.Token.WaitHandle.WaitOne(PollIntervalMs))
			{
				if (Debugger.IsAttached || Volatile.Read(ref _heartbeat) is 1)
					missCount = 0;
				else if (++missCount >= TimeoutMissCount)
				{
					var elapsedMs = missCount * PollIntervalMs;
					var message = string.Create(CultureInfo.InvariantCulture,
						$"Watchdog timeout: Main thread missed {missCount} heartbeat(s) in ~{elapsedMs} msec");

					try
					{
						Log.Fatal("{Message}", message);
						Log.CloseAndFlush();
					}
					finally
					{
						Environment.FailFast(message);
					}

					return;
				}

				Volatile.Write(ref _heartbeat, 0);
			}
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			Environment.FailFast(exception.ToString(), exception);
		}
	}
}
