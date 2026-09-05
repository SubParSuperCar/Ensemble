#if EXPORT
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Root;

public static class ExportPatch
{
	[ModuleInitializer]
	public static void Initialize()
	{
		try
		{
			Console.WriteLine("Patching export layout...");
			var stopwatch = Stopwatch.StartNew();

			var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
			if (exeDir is null)
				return;

			foreach (var dataDir in Directory.EnumerateDirectories(exeDir, "data*", SearchOption.TopDirectoryOnly))
				MoveContents(dataDir, exeDir);

			stopwatch.Stop();
			Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
				$"Patched export layout in {stopwatch.Elapsed.TotalMilliseconds:F3} ms."));
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
