using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Hardware.Info;
using Root.Autoloading;
using Root.Common.Utils;
using Serilog;
using Environment = System.Environment;
using Kvp = (string Key, string Value);

namespace Root.Scripts.Logging;

[GlobalClass]
[Autoload(Order = 2, FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class DiagnosticLogger : Node, IAutoload
{
	private const string LinuxKernelVersionFilePath = "/proc/sys/kernel/osrelease";

	public void Initialize() =>
		_ = Task.Run(() =>
		{
			Log.Debug("Building {Class} report...", nameof(DiagnosticLogger));
			var stopwatch = Stopwatch.StartNew();

			var lines = new List<Kvp>();

			AddSoftwareInfo(lines);
			AddHardwareInfo(lines);
			AddLocaleInfo(lines);

			Log.Information("\n{Report}", BuildReport(lines));

			stopwatch.Stop();
			Log.Debug("Built {Class} report in {ElapsedMs:F3} ms.",
				nameof(DiagnosticLogger), stopwatch.Elapsed.TotalMilliseconds);
		});

	private static void AddSoftwareInfo(List<Kvp> lines)
	{
		Add(lines, "Machine Name", Environment.MachineName);
		Add(lines, "User Name", Environment.UserName);

		Add(lines, "OS", RuntimeInformation.OSDescription);
		Add(lines, "OS Arch.", RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant());
		Add(lines, ".NET", RuntimeInformation.FrameworkDescription);

		Add(lines, "Build Config.",
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

		Add(lines, "Build Version", (string)ProjectSettings.GetSetting("application/config/version", "Unknown"));
		Add(lines, "Build Time", BuildInfo.BuildTime);

		if (OperatingSystem.IsLinux())
		{
			try
			{
				if (File.Exists(LinuxKernelVersionFilePath))
					Add(lines, "Kernel", File.ReadAllText(LinuxKernelVersionFilePath));
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Failed to read Linux kernel version file at: {Path}",
					LinuxKernelVersionFilePath);
			}

			Add(lines, "Shell", Environment.GetEnvironmentVariable("SHELL"));
			Add(lines, "Desktop", Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));
			Add(lines, "Session", Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"));
		}

		Add(lines, "System Uptime",
			TimeSpan.FromMilliseconds(Environment.TickCount64)
				.ToString(@"d\d\ hh\h\ mm\m", CultureInfo.InvariantCulture));
	}

	private static void AddHardwareInfo(List<Kvp> lines)
	{
		var hwInfo = new HardwareInfo();
		hwInfo.RefreshCPUList();

		foreach (var cpu in hwInfo.CpuList)
		{
			Add(lines, "CPU", cpu.Name);
			Add(lines, "Topology", $"{cpu.NumberOfCores}C / {cpu.NumberOfLogicalProcessors}T");

			if (cpu.MaxClockSpeed > 0)
				Add(lines, "Max Clock",
					string.Create(CultureInfo.InvariantCulture, $"{cpu.MaxClockSpeed / 1000f:F2} GHz"));
		}

		hwInfo.RefreshVideoControllerList();
		foreach (var gpu in hwInfo.VideoControllerList)
			Add(lines, "GPU", gpu.Name);

		hwInfo.RefreshMemoryStatus();
		var totalMemory = hwInfo.MemoryStatus.TotalPhysical;
		var usedMemory = totalMemory - hwInfo.MemoryStatus.AvailablePhysical;
		Add(lines, "Memory", $"{Formatter.FormatBytes(usedMemory)} / {Formatter.FormatBytes(totalMemory)}");

		hwInfo.RefreshMotherboardList();
		if (hwInfo.MotherboardList.FirstOrDefault() is { } board)
			Add(lines, "Board", $"{board.Manufacturer} {board.Product}");

		hwInfo.RefreshBIOSList();
		if (hwInfo.BiosList.FirstOrDefault() is { } bios)
			Add(lines, "BIOS", $"{bios.Manufacturer} {bios.Version}");

		hwInfo.RefreshDriveList();
		foreach (var drive in hwInfo.DriveList.OrderBy(d => d.Model, StringComparer.OrdinalIgnoreCase))
			Add(lines, "Drive", $"{drive.Model} ({Formatter.FormatBytes(drive.Size)})");

		hwInfo.RefreshMonitorList();
		foreach (var monitor in hwInfo.MonitorList)
			Add(lines, "Monitor", monitor.Name);

		hwInfo.RefreshNetworkAdapterList();
		foreach (
			var nic in hwInfo.NetworkAdapterList
				.Where(a => !string.IsNullOrWhiteSpace(a.Name) && a.Name is not "lo")
				.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
			Add(lines, "NIC", nic.Name);
	}

	private static void AddLocaleInfo(List<Kvp> lines)
	{
		Add(lines, "Culture", CultureInfo.CurrentCulture.DisplayName);
		Add(lines, "Time Zone", TimeZoneInfo.Local.DisplayName);
	}

	private static string BuildReport(List<Kvp> lines)
	{
		var builder = new StringBuilder();
		builder.AppendLine("=== System Info (Diagnostics) ===");

		var width = lines.Max(line => line.Key.Length);

		foreach (var (key, value) in lines)
			builder.AppendLine(CultureInfo.InvariantCulture, $"{key.PadRight(width)} : {value}");

		return builder.ToString().TrimEnd();
	}

	private static void Add(List<Kvp> lines, string key, object? value)
	{
		var text = value?.ToString();

		if (!string.IsNullOrWhiteSpace(text))
			lines.Add((key, text.Trim()));
	}
}
