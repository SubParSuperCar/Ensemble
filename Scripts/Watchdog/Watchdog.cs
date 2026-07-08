using System.Diagnostics;
using System.Globalization;
using Godot;
using Environment = System.Environment;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Watchdog;

public partial class Watchdog : Node
{
	private const int PollIntervalMs = 1000;
	private const int MaxMissCount = 15;

	private static long _heartbeatCount;

	private CancellationTokenSource _cts = null!;
	private Thread _pollThread = null!;

	public static Watchdog? Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;

		Interlocked.Exchange(ref _heartbeatCount, 0);
		_cts = new CancellationTokenSource();

		Heartbeat();

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
		if (Input.IsActionJustPressedByEvent("hang", @event))
			Thread.Sleep(int.MaxValue);
	}

	public static void Heartbeat() => Interlocked.Increment(ref _heartbeatCount);

	private void PollLoop()
	{
		var token = _cts.Token;

		var missCount = 0;
		var lastHeartbeatCount = Interlocked.Read(ref _heartbeatCount);

		try
		{
			while (!token.WaitHandle.WaitOne(PollIntervalMs))
			{
				if (Debugger.IsAttached)
				{
					missCount = 0;
					lastHeartbeatCount = Interlocked.Read(ref _heartbeatCount);

					continue;
				}

				var heartbeatCount = Interlocked.Read(ref _heartbeatCount);
				missCount = heartbeatCount == lastHeartbeatCount ? missCount + 1 : 0;
				lastHeartbeatCount = heartbeatCount;

				if (missCount < MaxMissCount)
					continue;

				Environment.FailFast(string.Create(CultureInfo.InvariantCulture,
					$"Main thread missed {missCount} heartbeats in ~{missCount * PollIntervalMs} ms"));

				return;
			}
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			Environment.FailFast(exception.ToString(), exception);
		}
	}
}
