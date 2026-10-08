#if EXPORT && !ENSEMBLE_JIT
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace EnsembleRoot;

/// <summary>
///     Flattens NativeAOT exports by hoisting the <c>data_*</c> directory's contents beside the executable.
/// </summary>
/// <remarks>
///     Excluded from JIT exports, where it would move the .NET runtime out from under the host mid-load,
///     and skipped on macOS, where the data lives in the signed bundle's <c>Resources</c> directory.
/// </remarks>
public static class PatchExport
{
	[ModuleInitializer]
	public static void Initialize()
	{
		if (OperatingSystem.IsMacOS() || Path.GetDirectoryName(Environment.ProcessPath) is not { } exeDir)
			return;

		try
		{
			Console.WriteLine("Patching export layout...");
			var start = Stopwatch.GetTimestamp();

			foreach (var dataDir in Directory.EnumerateDirectories(exeDir, "data*", SearchOption.TopDirectoryOnly))
				MoveContents(dataDir, exeDir);

			Console.WriteLine(string.Create(
				CultureInfo.InvariantCulture,
				$"Patched export layout in {Stopwatch.GetElapsedTime(start).TotalMilliseconds:F3} ms"));
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine($"Failed to patch export layout:\n{exception}");
		}
	}

	private static void MoveContents(string source, string destination)
	{
		foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
		{
			var target = Path.Combine(destination, Path.GetRelativePath(source, file));
			Directory.CreateDirectory(Path.GetDirectoryName(target)!);
			File.Move(file, target, true);
		}
	}
}

#endif
