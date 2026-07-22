using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Root.Globals;
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

	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	protected override void OnDispose() => _dispatcher.Process -= OnProcess;

	private void OnProcess(double delta)
	{
		var tick = Time.GetTicksMsec();
		if (tick - _lastTick < TimeSpan.MillisecondsPerSecond) return;
		_lastTick = tick;

		var fps = Performance.GetMonitor(Performance.Monitor.TimeFps);

		var stats = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["Frame Rate"] = string.Create(CultureInfo.InvariantCulture,
				$"{fps} FPS ({(fps > 0 ? TimeSpan.MillisecondsPerSecond / fps : double.PositiveInfinity):F3} mspf)"),
			["Process Time"] = string.Create(CultureInfo.InvariantCulture,
				$"{Performance.GetMonitor(Performance.Monitor.TimeProcess) * TimeSpan.MillisecondsPerSecond:F3} msec"),
			["Physics Time"] = string.Create(CultureInfo.InvariantCulture,
				$"{Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * TimeSpan.MillisecondsPerSecond:F3} msec"),
			["Used Static Memory (DRAM)"] =
				Util.FormatBytes((ulong)Performance.GetMonitor(Performance.Monitor.MemoryStatic)),
			["Used Video Memory (VRAM)"] =
				Util.FormatBytes((ulong)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed)),
			["Object Count"] =
				Performance.GetMonitor(Performance.Monitor.ObjectCount),
			["Node Count"] =
				Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
			["Orphan Node Count"] =
				Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount),
			["Draw Call Count"] =
				Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),
			["Render Primitive Count"] =
				Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame)
		};

		var width = stats.Keys.Max(k => k.Length);
		Text = string.Join(Environment.NewLine, stats.Select(kvp => $"{kvp.Key.PadRight(width)} = {kvp.Value}"));
	}
}
