using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Godot;

namespace Root.Ui.Impl.ViewModels;

public partial class StatViewModel : ViewModelBase
{
	private const int LabelWidth = 21;

	private static readonly string[] Units = ["B", "KiB", "MiB", "GiB", "TiB"];

	private static readonly string Fps = "FPS".PadRight(LabelWidth);
	private static readonly string TimeProcess = "Time: Process".PadRight(LabelWidth);
	private static readonly string TimePhysics = "Time: Physics".PadRight(LabelWidth);
	private static readonly string RenderDrawCalls = "Render: Draw Calls".PadRight(LabelWidth);
	private static readonly string RenderPrimitives = "Render: Primitives".PadRight(LabelWidth);
	private static readonly string MemoryStatic = "Memory: Static (DRAM)".PadRight(LabelWidth);
	private static readonly string MemoryVideo = "Memory: Video (VRAM)".PadRight(LabelWidth);
	private static readonly string Objects = "Objects".PadRight(LabelWidth);
	private static readonly string Nodes = "Nodes".PadRight(LabelWidth);
	private static readonly string NodesOrphan = "Nodes: Orphan".PadRight(LabelWidth);

	private readonly StringBuilder _sb = new(512);

	public StatViewModel()
	{
		Dispatcher.Process += OnProcess;
	}

	// ReSharper disable once MemberCanBeMadeStatic.Global
	[ObservableProperty] public partial string Text { get; set; } = string.Empty;

	protected override void OnDispose() => Dispatcher.Process -= OnProcess;

	private void OnProcess(double delta)
	{
		var fps = Performance.GetMonitor(Performance.Monitor.TimeFps);
		var culture = CultureInfo.InvariantCulture;

		_sb.Clear();
		_sb.Append(Fps).Append(" = ").Append(culture, $"{fps:F0} ({1000 / fps:F3} ms)").AppendLine();
		_sb.Append(TimeProcess).Append(" = ")
			.Append(culture, $"{Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000:F3} ms").AppendLine();
		_sb.Append(TimePhysics).Append(" = ")
			.Append(culture, $"{Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000:F3} ms")
			.AppendLine();
		_sb.Append(RenderDrawCalls).Append(" = ")
			.Append(culture, $"{Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame):F0}")
			.AppendLine();
		_sb.Append(RenderPrimitives).Append(" = ")
			.Append(culture, $"{Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame):F0}")
			.AppendLine();
		_sb.Append(MemoryStatic).Append(" = ")
			.AppendLine(FormatBytes((long)Performance.GetMonitor(Performance.Monitor.MemoryStatic)));
		_sb.Append(MemoryVideo).Append(" = ")
			.AppendLine(FormatBytes((long)Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed)));
		_sb.Append(Objects).Append(" = ")
			.Append(culture, $"{Performance.GetMonitor(Performance.Monitor.ObjectCount):F0}")
			.AppendLine();
		_sb.Append(Nodes).Append(" = ")
			.Append(culture, $"{Performance.GetMonitor(Performance.Monitor.ObjectNodeCount):F0}")
			.AppendLine();
		_sb.Append(NodesOrphan).Append(" = ")
			.Append(culture, $"{Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount):F0}");

		Text = _sb.ToString();
	}

	private static string FormatBytes(long bytes)
	{
		double value = bytes;
		var unit = 0;

		while (value >= 1024 && unit < Units.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		return string.Create(CultureInfo.InvariantCulture, $"{value:F3} {Units[unit]}");
	}
}
