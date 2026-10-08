using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace EnsembleRoot.Scripts.Logging.Impl;

/// <summary>
///     Describes an exception's frames as module offsets (e.g., <c>EnsembleGame.so+0x1a2b3c</c>) under NativeAOT,
///     whose stack traces have no line numbers.
/// </summary>
/// <remarks>
///     Offsets are return addresses; symbolize <c>offset - 1</c> against the build's debug symbols, e.g.,
///     <c lang="shell">addr2line -fCi -e EnsembleGame.so.dbg 0x1a2b3b</c>.
/// </remarks>
public static class NativeFrames
{
	public const string PropertyName = "NativeFrames";

	private static readonly string ModuleName =
		"EnsembleGame" + (OperatingSystem.IsWindows() ? ".dll" : OperatingSystem.IsMacOS() ? ".dylib" : ".so");

	private static bool IsSupported => !RuntimeFeature.IsDynamicCodeSupported;

	public static string? Describe(Exception exception)
	{
		if (!IsSupported)
			return null;

		var lines = new List<string>();

		for (var current = exception; current is not null; current = current.InnerException)
		{
			lines.Add(current.GetType().FullName ?? current.GetType().Name);
			lines.AddRange(new StackTrace(current, false)
				.GetFrames()
				.Where(static frame => frame.HasNativeImage())
				.Select(static frame => string.Create(
					CultureInfo.InvariantCulture,
					$"   at {ModuleName}+0x{frame.GetNativeIP() - frame.GetNativeImageBase():x}")));
		}

		return string.Join('\n', lines);
	}
}
