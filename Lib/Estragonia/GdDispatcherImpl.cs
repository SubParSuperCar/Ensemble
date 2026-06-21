using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Avalonia.Threading;
using Godot;
using GdDispatcher = Godot.Dispatcher;
using SysTimer = System.Threading.Timer;

namespace Estragonia;

/// <summary>An implementation of <see cref="IDispatcherImpl" /> that uses the underlying Godot dispatcher.</summary>
[SuppressMessage(
	"Design",
	"CA1001:Types that own disposable fields should be disposable",
	Justification = "This type has equivalent to a static lifetime"
)]
internal sealed class GodotDispatcherImpl : IDispatcherImpl
{
	private readonly SendOrPostCallback _invokeSignaled; // cached delegate
	private readonly SendOrPostCallback _invokeTimer; // cached delegate

	private readonly Thread _mainThread;
	private readonly SysTimer _timer;

	public GodotDispatcherImpl(Thread mainThread)
	{
		_mainThread = mainThread;
		_invokeSignaled = InvokeSignaled;
		_invokeTimer = InvokeTimer;
		_timer = new SysTimer(OnTimerTick, this, Timeout.Infinite, Timeout.Infinite);
	}

	public long Now
		=> (long)Time.GetTicksMsec();

	public bool CurrentThreadIsLoopThread
		=> _mainThread == Thread.CurrentThread;

	public event Action? Signaled;

	public event Action? Timer;

	public void UpdateTimer(long? dueTimeInMs)
	{
		var interval = dueTimeInMs is { } value
			? Math.Clamp(value - Now, 0L, 0xFFFFFFFEL)
			: Timeout.Infinite;

		_timer.Change(interval, Timeout.Infinite);
	}

	public void Signal() => GdDispatcher.SynchronizationContext.Post(_invokeSignaled, this);

	private void OnTimerTick(object? state) => GdDispatcher.SynchronizationContext.Post(_invokeTimer, null);

	private void InvokeSignaled(object? state) => Signaled?.Invoke();

	private void InvokeTimer(object? state) => Timer?.Invoke();
}
