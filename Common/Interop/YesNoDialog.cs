using System.Runtime.InteropServices;
using TinyDialogsNet;

namespace EnsembleRoot.Common.Interop;

/// <summary>A native yes / no message box, through the tinyfd library TinyDialogsNet bundles.</summary>
/// <remarks>
///     TinyDialogsNet 2.0.0 passes tinyfd Yes as the default for <see cref="MessageBoxButton.No" /> in yes / no boxes,
///     so Enter answered Yes; this sets the default itself. Windows and macOS honor it, but tinyfd can't set one for
///     zenity or kdialog, which focus Yes. It blocks the calling thread until answered, and a box that can't open
///     (e.g., without a dialog tool on Linux) answers No. tinyfd rejects quotes, so the message drops them.
/// </remarks>
internal static class YesNoDialog
{
	private const string LibraryName = "tinyfiledialogs";
	private const string DialogType = "yesno";

	/// <exception cref="ArgumentException">The title contains a line break, a tab, or a quote.</exception>
	public static bool Ask(string title, string message, MessageBoxIconType icon, bool isYesDefault)
	{
		if (title.AsSpan().ContainsAny("\n\t'\""))
			throw new ArgumentException("Titles can't contain line breaks, tabs, or quotes.", nameof(title));

		var iconName = icon switch
		{
			MessageBoxIconType.Information => "info",
			MessageBoxIconType.Warning => "warning",
			MessageBoxIconType.Error => "error",
			MessageBoxIconType.Question => "question",
			_ => throw new ArgumentOutOfRangeException(nameof(icon), icon, null)
		};

		var defaultButton = isYesDefault ? 1 : 0;
		message = RemoveQuotes(message);

		var response = OperatingSystem.IsWindows()
			? MessageBoxUtf16(title, message, DialogType, iconName, defaultButton)
			: MessageBoxUtf8(title, message, DialogType, iconName, defaultButton);

		return response is 1;
	}

	private static string RemoveQuotes(string text) =>
		text
			.Replace("\"", string.Empty, StringComparison.Ordinal)
			.Replace("'", string.Empty, StringComparison.Ordinal)
			.Replace("`", string.Empty, StringComparison.Ordinal);

#pragma warning disable CA2101, SYSLIB1054
	[DllImport(LibraryName, EntryPoint = "tinyfd_messageBox", CallingConvention = CallingConvention.Cdecl)]
	private static extern int MessageBoxUtf8(
		[MarshalAs(UnmanagedType.LPUTF8Str)] string title, [MarshalAs(UnmanagedType.LPUTF8Str)] string message,
		[MarshalAs(UnmanagedType.LPUTF8Str)] string dialogType, [MarshalAs(UnmanagedType.LPUTF8Str)] string iconType,
		int defaultButton);

	[DllImport(LibraryName, EntryPoint = "tinyfd_messageBoxW", CallingConvention = CallingConvention.Cdecl)]
	private static extern int MessageBoxUtf16(
		[MarshalAs(UnmanagedType.LPWStr)] string title, [MarshalAs(UnmanagedType.LPWStr)] string message,
		[MarshalAs(UnmanagedType.LPWStr)] string dialogType, [MarshalAs(UnmanagedType.LPWStr)] string iconType,
		int defaultButton);
#pragma warning restore CA2101, SYSLIB1054
}
