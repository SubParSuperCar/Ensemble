using System.Reflection;
using System.Runtime.InteropServices;
using HarfBuzzSharp;

namespace EnsembleRoot.Common.Interop;

/// <summary>
///     Loads HarfBuzzSharp's bundled HarfBuzz with <c>RTLD_DEEPBIND</c> on Linux, so it always calls its own
///     functions.
/// </summary>
/// <remarks>
///     System FreeType (used by SkiaSharp, and by distro Godot builds) loads the system HarfBuzz globally on demand,
///     after which the bundled one's lazily bound calls resolve to it. Destroying a font then runs another HarfBuzz
///     version's code on the bundled one's data, crashing the process (e.g., when Markdown SVG badges are shaped).
/// </remarks>
internal static class HarfBuzzIsolation
{
	private const string LibraryName = "libHarfBuzzSharp";
	private const string LibraryFileName = LibraryName + ".so";

	private const int RtldNow = 0x2;
	private const int RtldDeepBind = 0x8;

	private static readonly Lazy<nint> Handle = new(Load);

	public static void Apply()
	{
		if (!OperatingSystem.IsLinux())
			return;

		try
		{
			NativeLibrary.SetDllImportResolver(typeof(Blob).Assembly, Resolve);
		}
		catch (InvalidOperationException exception)
		{
			Console.WriteLine($"Failed to isolate {LibraryName}: {exception.Message}");
		}
	}

	private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) =>
		libraryName.StartsWith(LibraryName, StringComparison.Ordinal) ? Handle.Value : 0;

	private static nint Load()
	{
		var assemblyDirectory = Path.GetDirectoryName(typeof(Blob).Assembly.Location);
		var rid = "linux-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

		var paths = new[] { assemblyDirectory, AppContext.BaseDirectory }
			.OfType<string>()
			.Where(static directory => directory.Length is not 0)
			.SelectMany(directory => new[] { Path.Combine(directory, "runtimes", rid, "native"), directory })
			.Select(static directory => Path.Combine(directory, LibraryFileName))
			.Where(File.Exists);

		try
		{
			foreach (var path in paths)
				if (DlOpen(path, RtldNow | RtldDeepBind) is var handle and not 0)
					return handle;
		}
		catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException) { }

		Console.WriteLine($"Failed to isolate {LibraryName}; falling back to default loading");
		return 0;
	}

#pragma warning disable CA2101, SYSLIB1054
	[DllImport("libc.so.6", EntryPoint = "dlopen")]
	private static extern nint DlOpen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);
#pragma warning restore CA2101, SYSLIB1054
}
