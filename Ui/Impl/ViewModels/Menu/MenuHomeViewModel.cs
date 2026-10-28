using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Common.Text;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Services;
using Serilog;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using FileAccess = Godot.FileAccess;
using OS = Godot.OS;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class MenuHomeViewModel : ViewModelBase
{
	internal const int MaxReasonLength = 128;

	private readonly NavigatorService _navigator;

	public MenuHomeViewModel(NavigatorService navigator)
	{
		_navigator = navigator;

		GSessionManager.SessionFailed += OnSessionFailed;
	}

	// Loaded once and shared, as menu pages are recreated on every navigation
	public static Bitmap? Icon { get; } = LoadBitmap(GameIconPath);

	public static string GitHubPageLabel => $"Ensemble - GitHub Page\ngithub.com/{GitHubRepoPath}";

	[ObservableProperty] public partial string? Notice { get; private set; }

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
		Notice = $"Session ended: {TextElements.Truncate(reason, MaxReasonLength)}";

	private static Bitmap? LoadBitmap(string path)
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
}
