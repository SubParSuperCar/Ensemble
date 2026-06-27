using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Godot;
using Environment = System.Environment;

namespace Root.Ui.Impl.ViewModels;

public partial class StatViewModel : ViewModelBase
{
	private ulong _lastProcess;

	public StatViewModel()
	{
		Dispatcher.Process += OnProcess;
	}

	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	public override void Dispose()
	{
		Dispatcher.Process -= OnProcess;
		GC.SuppressFinalize(this);
	}

	private void OnProcess(double delta)
	{
		var ticks = Time.GetTicksMsec();
		if (ticks - _lastProcess < 1000) return;
		_lastProcess = ticks;

		var fps = Performance.GetMonitor(Performance.Monitor.TimeFps);

		var stats = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
		{
			["FPS"] = string.Create(CultureInfo.InvariantCulture, $"{fps} ({1000 / fps:F3} ms)"),
			["Time: Process"] = string.Create(CultureInfo.InvariantCulture,
				$"{Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000:F3} ms"),
			["Time: Physics"] = string.Create(CultureInfo.InvariantCulture,
				$"{Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000:F3} ms"),
			["Render: Draw Calls"] = Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),
			["Render: Primitives"] = Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame),
			["Memory: Static (DRAM)"] = FormatBytes((long)Performance.GetMonitor(Performance.Monitor.MemoryStatic)),
			["Memory: Video (VRAM)"] =
				FormatBytes((long)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed)),
			["Objects"] = Performance.GetMonitor(Performance.Monitor.ObjectCount),
			["Nodes"] = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
			["Nodes: Orphan"] = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)
		};

		var width = stats.Keys.Max(k => k.Length);
		Text = string.Join(Environment.NewLine, stats.Select(kvp => $"{kvp.Key.PadRight(width)} = {kvp.Value}"));
	}

	private static string FormatBytes(long bytes)
	{
		string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];

		double value = bytes;
		var unit = 0;

		while (value >= 1024 && unit < units.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		return string.Create(CultureInfo.InvariantCulture, $"{value:F3} {units[unit]}");
	}
}
