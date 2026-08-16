using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Hardware.Info;
using Root.Common.Util;
using Serilog;
using Environment = System.Environment;

namespace Root.Scripts.Logger;

[GlobalClass]
public partial class SysInfoLogger : Node
{
	private const string LinuxKernelVersionFilePath = "/proc/sys/kernel/osrelease";

	public override void _Ready()
	{
		var lines = new List<(string Key, string Value)>();

		AddSoftwareInfo(lines);
		AddHardwareInfo(lines);

		Add(lines, "Culture", CultureInfo.CurrentCulture.DisplayName);
		Add(lines, "Time Zone", TimeZoneInfo.Local.DisplayName);

		Log.Information("{SysInfo}", Environment.NewLine + BuildReport(lines));

		QueueFree();
	}

	private static void AddSoftwareInfo(List<(string Key, string Value)> lines)
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
#elif ENSEMBLE_DEBUG
			"EXPORT DEBUG"
#elif RELEASE
			"RELEASE"
#elif ENSEMBLE_RELEASE
			"EXPORT RELEASE"
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
					Add(lines, "Kernel", File.ReadAllText(LinuxKernelVersionFilePath));
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

	private static void AddHardwareInfo(List<(string Key, string Value)> lines)
	{
		var hardwareInfo = new HardwareInfo();
		hardwareInfo.RefreshAll(); // TODO: Only refresh used members

		foreach (var cpu in hardwareInfo.CpuList)
		{
			Add(lines, "CPU", cpu.Name);
			Add(lines, "Topology", $"{cpu.NumberOfCores}C / {cpu.NumberOfLogicalProcessors}T");

			if (cpu.MaxClockSpeed > 0)
				Add(lines, "Max Clock",
					string.Create(CultureInfo.InvariantCulture, $"{cpu.MaxClockSpeed / 1000f:F2} GHz"));
		}

		Add(lines, "Endianness", BitConverter.IsLittleEndian ? "Little" : "Big");

		foreach (var gpu in hardwareInfo.VideoControllerList)
			Add(lines, "GPU", gpu.Name);

		var totalMemory = hardwareInfo.MemoryStatus.TotalPhysical;
		var usedMemory = totalMemory - hardwareInfo.MemoryStatus.AvailablePhysical;
		Add(lines, "Memory", $"{Formatter.FormatBytes(usedMemory)} / {Formatter.FormatBytes(totalMemory)}");

		if (hardwareInfo.MotherboardList.FirstOrDefault() is { } board)
			Add(lines, "Board", $"{board.Manufacturer} {board.Product}");

		if (hardwareInfo.BiosList.FirstOrDefault() is { } bios)
			Add(lines, "BIOS", $"{bios.Manufacturer} {bios.Version}");

		foreach (var drive in hardwareInfo.DriveList.OrderBy(d => d.Model, StringComparer.OrdinalIgnoreCase))
			Add(lines, "Drive", $"{drive.Model} ({Formatter.FormatBytes(drive.Size)})");

		foreach (var monitor in hardwareInfo.MonitorList)
			Add(lines, "Monitor", monitor.Name);

		foreach (var adapter in hardwareInfo.NetworkAdapterList
			         .Where(a => !string.IsNullOrWhiteSpace(a.Name) && a.Name is not "lo")
			         .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
			Add(lines, "Network", adapter.Name);
	}

	private static string BuildReport(List<(string Key, string Value)> lines)
	{
		var builder = new StringBuilder();
		builder.AppendLine("=== System Information ===");

		var width = lines.Max(line => line.Key.Length);

		foreach (var (key, value) in lines)
			builder.AppendLine(CultureInfo.InvariantCulture, $"{key.PadRight(width)} : {value}");

		return builder.ToString().TrimEnd();
	}

	private static void Add(List<(string Key, string Value)> lines, string key, object? value)
	{
		var text = value?.ToString();

		if (!string.IsNullOrWhiteSpace(text))
			lines.Add((key, text.Trim()));
	}
}
