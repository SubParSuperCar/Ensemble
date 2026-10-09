using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using Serilog;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using FileAccess = Godot.FileAccess;
using OS = Godot.OS;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public sealed partial class MenuHomeViewModel : ViewModelBase
{
	private const int MaxReasonLength = 128;

	private readonly NavigatorService _navigator;

	public MenuHomeViewModel(NavigatorService navigator)
	{
		_navigator = navigator;

		GSessionManager.SessionFailed += OnSessionFailed;
	}

	// Loaded once and shared, as menu pages are recreated on every navigation
	public static Bitmap? Icon { get; } = LoadBitmapFromGodotImage(GameIconPath);

	[ObservableProperty] public partial string? Notice { get; set; }

	protected override void OnDispose() => GSessionManager.SessionFailed -= OnSessionFailed;

	[RelayCommand]
	private static void OpenGitHubPage() => OS.ShellOpen(GitHubRepoUrl);

	[RelayCommand]
	private void GoToSession() => _navigator.GoTo<SessionModeSelectorViewModel>();

	[RelayCommand]
	private void GoToDocFileViewer() => _navigator.GoTo<MenuDocFileViewModel>();

	[RelayCommand]
	private void GoToWebBrowser() => _navigator.GoTo<MenuWebBrowserViewModel>();

	private void OnSessionFailed(string reason) =>
		Notice = string.Create(
			CultureInfo.InvariantCulture,
			$"Session Ended\nReason: \"{TruncateToTextElements(reason, MaxReasonLength)}\"\n" +
			$"Time: {GTimeProvider.GetLocalNow():h:mm:ss tt zz}");

	private static Bitmap? LoadBitmapFromGodotImage(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

		if (file is null)
		{
			Log.Warning("Failed to open {Path}: {Error}", path, FileAccess.GetOpenError());
			return null;
		}

		using var stream = new MemoryStream(file.GetBuffer((long)file.GetLength()));
		return new Bitmap(stream);
	}

	private static string TruncateToTextElements(string value, int maxLength)
	{
		if (maxLength <= 0)
			return string.Empty;

		var elements = StringInfo.ParseCombiningCharacters(value);

		if (elements.Length <= maxLength)
			return value;

		var end = elements[maxLength - 1];
		return value[..end] + '\u2026';
	}
}
