using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvWindow = Avalonia.Controls.Window;

namespace Estragonia;

/// <summary>
///     Minimal <see cref="IClassicDesktopStyleApplicationLifetime" /> for Godot mode.
///     Tracks open windows but doesn't manage application shutdown, which Godot handles.
/// </summary>
internal sealed class GodotApplicationLifetime : IClassicDesktopStyleApplicationLifetime, IDisposable
{
	private readonly List<AvWindow> _windows = [];
	private CombinedDisposable? _eventSubscription;

	// ReSharper disable once UnusedAutoPropertyAccessor.Global
	public string[]? Args { get; set; }

	public ShutdownMode ShutdownMode { get; set; }

	public AvWindow? MainWindow { get; set; }

	public IReadOnlyList<AvWindow> Windows => _windows;

	public bool TryShutdown(int exitCode = 0) => false;

	public void Shutdown(int exitCode = 0)
	{
	}

	public void Dispose()
	{
		_eventSubscription?.Dispose();
		_eventSubscription = null;
	}

	/// <summary>
	///     Subscribes to the global window open/close events to track the <see cref="Windows" /> list.
	///     Must be called before any window is created.
	/// </summary>
	public void Initialize()
	{
		var openedSubscription = AvWindow.WindowOpenedEvent.AddClassHandler(
			typeof(AvWindow),
			(sender, _) =>
			{
				if (sender is AvWindow window && !_windows.Contains(window))
					_windows.Add(window);
			}
		);

		var closedSubscription = AvWindow.WindowClosedEvent.AddClassHandler(
			typeof(AvWindow),
			(sender, _) =>
			{
				if (sender is AvWindow window)
					_windows.Remove(window);
			}
		);

		_eventSubscription = new CombinedDisposable(openedSubscription, closedSubscription);
	}

	private sealed class CombinedDisposable(IDisposable first, IDisposable second) : IDisposable
	{
		public void Dispose()
		{
			first.Dispose();
			second.Dispose();
		}
	}

	// Godot owns the application lifetime, so these events are implemented but never raised
#pragma warning disable CS0067
	public event EventHandler<ControlledApplicationLifetimeStartupEventArgs>? Startup;
	public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;
	public event EventHandler<ControlledApplicationLifetimeExitEventArgs>? Exit;
#pragma warning restore CS0067
}
