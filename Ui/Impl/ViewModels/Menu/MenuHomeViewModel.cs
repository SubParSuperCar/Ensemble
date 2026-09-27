using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.Services;
using Serilog;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using FileAccess = Godot.FileAccess;
using OS = Godot.OS;

namespace EnsembleRoot.Ui.Impl.ViewModels;

public partial class MenuHomeViewModel : ViewModelBase
{
	private readonly NavigatorService _navigator;

	public MenuHomeViewModel(NavigatorService navigator)
	{
		_navigator = navigator;
		GSessionManager.SessionFailed += OnSessionFailed;
	}

	[ObservableProperty] public partial Bitmap? Icon { get; set; } = LoadBitmapFromGodotImage(GameIconPath);
	[ObservableProperty] public partial string? Notice { get; set; }

	protected override void OnDispose() => GSessionManager.SessionFailed -= OnSessionFailed;

	[RelayCommand]
	private static void OpenGitHubPage() => OS.ShellOpen("https://github.com/SubParSuperCar/Ensemble");

	[RelayCommand]
	private void GoToSession() => _navigator.GoTo<SessionModeSelectorViewModel>();

	[RelayCommand]
	private void GoToDocFileViewer() => _navigator.GoTo<MenuDocFileViewModel>();

	[RelayCommand]
	private void GoToWebBrowser() => _navigator.GoTo<MenuWebBrowserViewModel>();

	private void OnSessionFailed(string reason) => Notice = $"Session ended: {reason}";

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
}
