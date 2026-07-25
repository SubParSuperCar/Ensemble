using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Hardware.Info;
using Root.Globals;
using Serilog;
using Environment = System.Environment;

namespace Root.Scripts.Info;

public partial class InfoLogger : Node
{
	private const string LinuxKernelVersionFilePath = "/proc/sys/kernel/osrelease";

#pragma warning disable MA0051
	public override void _Ready()
#pragma warning restore MA0051
	{
		var hw = new HardwareInfo();
		hw.RefreshAll();

		var lines = new List<(string Key, string Value)>();

		var cpu = hw.CpuList.FirstOrDefault();
		var gpu = hw.VideoControllerList.FirstOrDefault();
		var board = hw.MotherboardList.FirstOrDefault();
		var bios = hw.BiosList.FirstOrDefault();

		Add("Machine", Environment.MachineName);
		Add("User", Environment.UserName);

		Add("OS", RuntimeInformation.OSDescription);
		Add("OS Arch.", RuntimeInformation.OSArchitecture);
		Add("Process", RuntimeInformation.ProcessArchitecture);
		Add(".NET", RuntimeInformation.FrameworkDescription);

		Add("Config",
#if DEBUG
			"DEBUG"
#elif RELEASE
			"RELEASE"
#else
			"Unknown"
#endif
		);

		if (OperatingSystem.IsLinux())
		{
			try
			{
				if (File.Exists(LinuxKernelVersionFilePath))
					Add("Kernel", File.ReadAllText(LinuxKernelVersionFilePath).Trim());
			}
			catch
			{
				// Ignore
			}

			Add("Shell", Environment.GetEnvironmentVariable("SHELL"));
			Add("Desktop", Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));
			Add("Session", Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"));
		}

		Add("Uptime",
			TimeSpan.FromMilliseconds(Environment.TickCount64)
				.ToString(@"d\d\ hh\h\ mm\m", CultureInfo.InvariantCulture));

		if (cpu is not null)
		{
			Add("CPU", cpu.Name);
			Add("Topology", $"{cpu.NumberOfCores}C / {cpu.NumberOfLogicalProcessors}T");

			if (cpu.MaxClockSpeed > 0)
				Add("Max Clock", string.Create(CultureInfo.InvariantCulture, $"{cpu.MaxClockSpeed / 1000.0:F2} GHz"));
		}

		Add("Endianness", BitConverter.IsLittleEndian ? "Little" : "Big");

		if (gpu is not null)
			Add("GPU", gpu.Name);

		Add(
			"Memory",
			$"{Util.FormatBytes(hw.MemoryStatus.TotalPhysical - hw.MemoryStatus.AvailablePhysical)} / {Util.FormatBytes(hw.MemoryStatus.TotalPhysical)}");

		if (board is not null)
			Add("Board", $"{board.Manufacturer} {board.Product}");

		if (bios is not null)
			Add("BIOS", $"{bios.Manufacturer} {bios.Version}");

		foreach (var drive in hw.DriveList.OrderBy(d => d.Model, StringComparer.OrdinalIgnoreCase))
			Add("Drive", $"{drive.Model} ({Util.FormatBytes(drive.Size)})");

		foreach (var monitor in hw.MonitorList)
			Add("Monitor", monitor.Name);

		foreach (var nic in hw.NetworkAdapterList
			         .Where(n => !string.IsNullOrWhiteSpace(n.Name) && n.Name is not "lo")
			         .OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase))
			Add("Network", nic.Name);

		Add("Culture", CultureInfo.CurrentCulture.DisplayName);
		Add("Time Zone", TimeZoneInfo.Local.DisplayName);

		var width = lines.Max(l => l.Key.Length);

		var sb = new StringBuilder();

		sb.AppendLine("=== System Information ===");

		foreach (var line in lines)
			sb.AppendLine(CultureInfo.InvariantCulture, $"{line.Key.PadRight(width)} : {line.Value.Trim()}");

		Log.Information("{SystemInfo}", Environment.NewLine + sb.ToString().TrimEnd());

		QueueFree();
		return;

		void Add(string key, object? value)
		{
			if (value is null)
				return;

			var text = value.ToString();

			if (!string.IsNullOrWhiteSpace(text))
				lines.Add((key, text));
		}
	}
}
