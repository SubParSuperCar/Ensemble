using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using EnsembleRoot.Autoloading;
using EnsembleRoot.Common.Utils;
using Godot;
using Hardware.Info;
using Microsoft.Extensions.Logging;
using Serilog;
using Environment = System.Environment;
using Entry = (string Name, string Value);

namespace EnsembleRoot.Scripts.Logging;

[GlobalClass]
[Autoload(Order = AutoloadOrder.Standard + 4, FailurePolicy = AutoloadFailurePolicy.LogAndContinue)]
public partial class DiagnosticLogger : Node, IAutoload
{
	private const string LinuxKernelVersionFilePath = "/proc/sys/kernel/osrelease";

	public void Initialize() =>
		_ = Task.Run(static () =>
		{
			try
			{
				LogReport();
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Failed to build {Class} report", nameof(DiagnosticLogger));
			}
		});

	private static void LogReport()
	{
		Log.Debug("Building {Class} report...", nameof(DiagnosticLogger));
		var stopwatch = Stopwatch.StartNew();

		var entries = new List<Entry>();

		AddSoftwareInfo(entries);

		// Hardware.Info shells out or uses WMI, either of which can fail, so the rest of the report still goes out
		try
		{
			AddHardwareInfo(entries);
		}
		catch (Exception exception)
		{
			Log.Warning(exception, "Failed to collect hardware info");
		}

		AddLocaleInfo(entries);

		Log.Information("\n{Report}", BuildReport(entries));

		stopwatch.Stop();
		Log.Debug(
			"Built {Class} report in {ElapsedMs:F3} ms", nameof(DiagnosticLogger), stopwatch.Elapsed.TotalMilliseconds);
	}

	private static void AddSoftwareInfo(List<Entry> entries)
	{
		// Machine and user names are omitted on purpose: they could aid manual LAN discovery, but logs get shared
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

		Add(entries, "Build Comp.",
#if NATIVE_AOT
			"Native Ahead-Of-Time (AOT)"
#else
			"Just-In-Time (JIT)"
#endif
		);

		Add(entries, "Build Version", GameVersion);
		Add(entries, "Build Commit", GameCommit);
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
				Log.Warning(
					exception, "Failed to read the Linux kernel version from {Path}", LinuxKernelVersionFilePath);
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
		var hardware = new HardwareInfo(logger: new Logger<HardwareInfo>(Logger.Factory!));
		hardware.RefreshCPUList(false, includePerformanceCounter: false);

		foreach (var cpu in hardware.CpuList)
		{
			Add(entries, "CPU", cpu.Name);
			Add(entries, "Topology", $"{cpu.NumberOfCores}C / {cpu.NumberOfLogicalProcessors}T");

			if (cpu.MaxClockSpeed > 0)
				Add(entries, "Max Clock",
					string.Create(CultureInfo.InvariantCulture, $"{cpu.MaxClockSpeed / 1000f:F2} GHz"));
		}

		hardware.RefreshVideoControllerList();
		foreach (var gpu in hardware.VideoControllerList)
			Add(entries, "GPU", gpu.Name);

		hardware.RefreshMemoryStatus();
		var totalMemory = hardware.MemoryStatus.TotalPhysical;
		var usedMemory = totalMemory - hardware.MemoryStatus.AvailablePhysical;
		Add(entries, "Memory", $"{ByteFormat.Humanize(usedMemory)} / {ByteFormat.Humanize(totalMemory)}");

		hardware.RefreshMotherboardList();
		if (hardware.MotherboardList.FirstOrDefault() is { } board)
			Add(entries, "Board", $"{board.Manufacturer} {board.Product}");

		hardware.RefreshBIOSList();
		if (hardware.BiosList.FirstOrDefault() is { } bios)
			Add(entries, "BIOS", $"{bios.Manufacturer} {bios.Version}");

		hardware.RefreshDriveList();
		foreach (var drive in hardware.DriveList.OrderBy(static drive => drive.Model, StringComparer.OrdinalIgnoreCase))
			Add(entries, "Drive", $"{drive.Model} ({ByteFormat.Humanize(drive.Size)})");

		hardware.RefreshMonitorList();
		foreach (var monitor in hardware.MonitorList)
			Add(entries, "Monitor", monitor.Name);

		hardware.RefreshNetworkAdapterList(false, false);
		var adapters = hardware.NetworkAdapterList
			.Where(static adapter => !string.IsNullOrWhiteSpace(adapter.Name) && adapter.Name is not "lo")
			.OrderBy(static adapter => adapter.Name, StringComparer.OrdinalIgnoreCase);
		foreach (var adapter in adapters)
			Add(entries, "NIC", adapter.Name);
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
