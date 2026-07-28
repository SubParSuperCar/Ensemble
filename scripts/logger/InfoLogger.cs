using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Hardware.Info;
using Root.Common.Util;
using Serilog;
using Environment = System.Environment;

namespace Root.Scripts.Info;

public partial class InfoLogger : Node
{
	private const string LinuxKernelVersionFilePath = "/proc/sys/kernel/osrelease";

	public override void _Ready()
	{
		var hw = new HardwareInfo();
		hw.RefreshAll(); // TODO: Only refresh used members

		var lines = new List<(string Key, string Value)>();

		AddGeneralInfo(lines);
		AddHardwareInfo(hw, lines);

		Add(lines, "Culture", CultureInfo.CurrentCulture.DisplayName);
		Add(lines, "Time Zone", TimeZoneInfo.Local.DisplayName);

		Log.Information("{SystemInfo}", Environment.NewLine + BuildReport(lines));

		QueueFree();
	}

	private static void AddGeneralInfo(List<(string Key, string Value)> lines)
	{
		Add(lines, "Machine", Environment.MachineName);
		Add(lines, "User", Environment.UserName);

		Add(lines, "OS", RuntimeInformation.OSDescription);
		Add(lines, "OS Arch.", RuntimeInformation.OSArchitecture);
		Add(lines, "Process", RuntimeInformation.ProcessArchitecture);
		Add(lines, ".NET", RuntimeInformation.FrameworkDescription);

		Add(lines, "Config",
#if DEBUG
			"DEBUG"
#elif RELEASE
			"RELEASE"
#else
			"Unknown"
#endif
		);

		Add(lines, "Build Time", BuildInfo.BuildTime);

		if (OperatingSystem.IsLinux())
		{
			try
			{
				if (File.Exists(LinuxKernelVersionFilePath))
					Add(lines, "Kernel", File.ReadAllText(LinuxKernelVersionFilePath).Trim());
			}
			catch
			{
				// Ignore
			}

			Add(lines, "Shell", Environment.GetEnvironmentVariable("SHELL"));
			Add(lines, "Desktop", Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));
			Add(lines, "Session", Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"));
		}

		Add(lines, "Uptime",
			TimeSpan.FromMilliseconds(Environment.TickCount64)
				.ToString(@"d\d\ hh\h\ mm\m", CultureInfo.InvariantCulture));
	}

	private static void AddHardwareInfo(HardwareInfo hw, List<(string Key, string Value)> lines)
	{
		var cpu = hw.CpuList.FirstOrDefault();
		var gpu = hw.VideoControllerList.FirstOrDefault();
		var board = hw.MotherboardList.FirstOrDefault();
		var bios = hw.BiosList.FirstOrDefault();

		if (cpu is not null)
		{
			Add(lines, "CPU", cpu.Name);
			Add(lines, "Topology", $"{cpu.NumberOfCores}C / {cpu.NumberOfLogicalProcessors}T");

			if (cpu.MaxClockSpeed > 0)
				Add(lines, "Max Clock", $"{cpu.MaxClockSpeed / 1000:F2} GHz");
		}

		Add(lines, "Endianness", BitConverter.IsLittleEndian ? "Little" : "Big");

		if (gpu is not null)
			Add(lines, "GPU", gpu.Name);

		Add(lines, "Memory",
			$"{Formatter.FormatBytes(hw.MemoryStatus.TotalPhysical - hw.MemoryStatus.AvailablePhysical)} / {Formatter.FormatBytes(hw.MemoryStatus.TotalPhysical)}");

		if (board is not null)
			Add(lines, "Board", $"{board.Manufacturer} {board.Product}");

		if (bios is not null)
			Add(lines, "BIOS", $"{bios.Manufacturer} {bios.Version}");

		foreach (var drive in hw.DriveList.OrderBy(d => d.Model, StringComparer.OrdinalIgnoreCase))
			Add(lines, "Drive", $"{drive.Model} ({Formatter.FormatBytes(drive.Size)})");

		foreach (var monitor in hw.MonitorList)
			Add(lines, "Monitor", monitor.Name);

		foreach (var nic in hw.NetworkAdapterList
					 .Where(n => !string.IsNullOrWhiteSpace(n.Name) && n.Name is not "lo")
					 .OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase))
			Add(lines, "Network", nic.Name);
	}

	private static string BuildReport(List<(string Key, string Value)> lines)
	{
		var sb = new StringBuilder();
		sb.AppendLine("=== System Information ===");

		var width = lines.Max(line => line.Key.Length);

		foreach (var (key, value) in lines)
			sb.AppendLine(CultureInfo.InvariantCulture, $"{key.PadRight(width)} : {value.Trim()}");

		return sb.ToString().TrimEnd();
	}

	private static void Add(List<(string Key, string Value)> lines, string key, object? value)
	{
		if (value is null)
			return;

		var text = value.ToString();

		if (!string.IsNullOrWhiteSpace(text))
			lines.Add((key, text));
	}
}
