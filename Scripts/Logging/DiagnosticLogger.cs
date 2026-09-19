using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Hardware.Info;
using Microsoft.Extensions.Logging;
using Root.Autoloading;
using Root.Common.Utils;
using Serilog;
using Environment = System.Environment;
using Entry = (string Name, string Value);

namespace Root.Scripts.Logging;

[GlobalClass]
[Autoload(Order = AutoloadOrder.Standard + 2, FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class DiagnosticLogger : Node, IAutoload
{
	private const string LinuxKernelVersionFilePath = "/proc/sys/kernel/osrelease";

	public void Initialize() =>
		_ = Task.Run(() =>
		{
			Log.Debug("Building {Class} report...", nameof(DiagnosticLogger));
			var stopwatch = Stopwatch.StartNew();

			var entries = new List<Entry>();

			AddSoftwareInfo(entries);
			AddHardwareInfo(entries);
			AddLocaleInfo(entries);

			Log.Information("\n{Report}", BuildReport(entries));

			stopwatch.Stop();
			Log.Debug(
				"Built {Class} report in {ElapsedMs:F3} ms",
				nameof(DiagnosticLogger),
				stopwatch.Elapsed.TotalMilliseconds);
		});

	private static void AddSoftwareInfo(List<Entry> entries)
	{
		Add(entries, "Machine Name", Environment.MachineName);
		Add(entries, "User Name", Environment.UserName);

		Add(entries, "OS", RuntimeInformation.OSDescription);
		Add(entries, "OS Arch.", RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant());
		Add(entries, ".NET", RuntimeInformation.FrameworkDescription);

		Add(entries, "Build Config.",
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

		Add(entries, "Build Version", (string)ProjectSettings.GetSetting("application/config/version", "Unknown"));
		Add(entries, "Build Time", BuildInfo.BuildTime);

		if (OperatingSystem.IsLinux())
		{
			try
			{
				if (File.Exists(LinuxKernelVersionFilePath))
					Add(entries, "Kernel", File.ReadAllText(LinuxKernelVersionFilePath));
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Failed to read Linux kernel version file at: {Path}", LinuxKernelVersionFilePath);
			}

			Add(entries, "Shell", Environment.GetEnvironmentVariable("SHELL"));
			Add(entries, "Desktop", Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"));
			Add(entries, "Session", Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"));
		}

		Add(entries, "System Uptime",
			TimeSpan.FromMilliseconds(Environment.TickCount64)
				.ToString(@"d\d\ hh\h\ mm\m", CultureInfo.InvariantCulture));
	}

	private static void AddHardwareInfo(List<Entry> entries)
	{
		var hwInfo = new HardwareInfo(logger: new Logger<HardwareInfo>(Logger.Factory!));
		hwInfo.RefreshCPUList(false, includePerformanceCounter: false);

		foreach (var cpu in hwInfo.CpuList)
		{
			Add(entries, "CPU", cpu.Name);
			Add(entries, "Topology", $"{cpu.NumberOfCores}C / {cpu.NumberOfLogicalProcessors}T");

			if (cpu.MaxClockSpeed > 0)
				Add(entries, "Max Clock",
					string.Create(CultureInfo.InvariantCulture, $"{cpu.MaxClockSpeed / 1000f:F2} GHz"));
		}

		hwInfo.RefreshVideoControllerList();
		foreach (var gpu in hwInfo.VideoControllerList)
			Add(entries, "GPU", gpu.Name);

		hwInfo.RefreshMemoryStatus();
		var totalMemory = hwInfo.MemoryStatus.TotalPhysical;
		var usedMemory = totalMemory - hwInfo.MemoryStatus.AvailablePhysical;
		Add(entries, "Memory", $"{Formatter.FormatBytes(usedMemory)} / {Formatter.FormatBytes(totalMemory)}");

		hwInfo.RefreshMotherboardList();
		if (hwInfo.MotherboardList.FirstOrDefault() is { } board)
			Add(entries, "Board", $"{board.Manufacturer} {board.Product}");

		hwInfo.RefreshBIOSList();
		if (hwInfo.BiosList.FirstOrDefault() is { } bios)
			Add(entries, "BIOS", $"{bios.Manufacturer} {bios.Version}");

		hwInfo.RefreshDriveList();
		foreach (var drive in hwInfo.DriveList.OrderBy(static drive => drive.Model, StringComparer.OrdinalIgnoreCase))
			Add(entries, "Drive", $"{drive.Model} ({Formatter.FormatBytes(drive.Size)})");

		hwInfo.RefreshMonitorList();
		foreach (var monitor in hwInfo.MonitorList)
			Add(entries, "Monitor", monitor.Name);

		hwInfo.RefreshNetworkAdapterList(false, false);
		foreach (
			var nic in hwInfo.NetworkAdapterList
				.Where(static adapter => !string.IsNullOrWhiteSpace(adapter.Name) && adapter.Name is not "lo")
				.OrderBy(static adapter => adapter.Name, StringComparer.OrdinalIgnoreCase))
			Add(entries, "NIC", nic.Name);
	}

	private static void AddLocaleInfo(List<Entry> entries)
	{
		Add(entries, "Culture", CultureInfo.CurrentCulture.DisplayName);
		Add(entries, "Time Zone", TimeZoneInfo.Local.DisplayName);
	}

	private static string BuildReport(IReadOnlyList<Entry> entries)
	{
		var builder = new StringBuilder();
		builder.AppendLine("=== Diagnostic Info ===");

		var width = entries.Max(static entry => entry.Name.Length);

		foreach (var (name, value) in entries)
			builder.AppendLine(CultureInfo.InvariantCulture, $"{name.PadRight(width)} : {value}");

		return builder.ToString().TrimEnd();
	}

	private static void Add(List<Entry> entries, string name, object? value)
	{
		var text = value?.ToString();

		if (!string.IsNullOrWhiteSpace(text))
			entries.Add((name, text.Trim()));
	}
}
