#if !DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Root.Common.Input;
using Root.Common.Util;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;
using Environment = System.Environment;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class StatViewModel : ViewModelBase
{
	private readonly DispatcherService _dispatcher;
	private ulong _lastTick;

	public StatViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;
		dispatcher.Process += OnProcess;
	}

	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	protected override void OnDispose() => _dispatcher.Process -= OnProcess;

	private void OnProcess(double delta)
	{
		var tick = Time.GetTicksMsec();
		if (tick - _lastTick < TimeSpan.MillisecondsPerSecond) return;
		_lastTick = tick;

		var fps = Performance.GetMonitor(Performance.Monitor.TimeFps);

#if DEBUG
		var dram = OS.GetStaticMemoryUsage();
#else
		using var process = Process.GetCurrentProcess();
		var dram = (ulong)process.PrivateMemorySize64;
#endif

		List<(string Key, object Value)> stats =
		[
			("Frame Rate", string.Create(CultureInfo.InvariantCulture,
				$"{fps} FPS ({(fps > 0 ? TimeSpan.MillisecondsPerSecond / fps : double.PositiveInfinity):F3} mspf)")),
			("Process Time", string.Create(CultureInfo.InvariantCulture,
				$"{Performance.GetMonitor(Performance.Monitor.TimeProcess) * TimeSpan.MillisecondsPerSecond:F3} msec")),
			("Physics Time", string.Create(CultureInfo.InvariantCulture,
				$"{Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * TimeSpan.MillisecondsPerSecond:F3} msec")),
			("Used DRAM", Formatter.FormatBytes(dram)),
			("Used VRAM",
				Formatter.FormatBytes((ulong)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed))),
			("Objects", Performance.GetMonitor(Performance.Monitor.ObjectCount)),
			("Nodes", Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)),
			("Orphan Nodes", Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)),
			("Draw Calls", Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)),
			("Render Prims.", Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)),
			("Input Sinking", InputExtensions.IsSunk)
		];

		if (GPlayers.Local is { } local && GPlayerManager.Handles.TryGetValue(local.Id, out var handle))
		{
			var character = handle.Character ?? handle.Controller!;

			stats.Add(("Char. Pos.", character.GlobalPosition.Round()));
			stats.Add(("Char. Speed",
				string.Create(CultureInfo.InvariantCulture, $"{character.GetRealVelocity().Length():F3} m/s")));
		}

		var width = stats.Max(s => s.Key.Length);
		Text = string.Join(Environment.NewLine, stats.Select(s => $"{s.Key.PadRight(width)} = {s.Value}"));
	}
}
