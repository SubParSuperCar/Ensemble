using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;
using Serilog;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using FileAccess = Godot.FileAccess;
using OS = Godot.OS;

namespace Root.Ui.Impl.ViewModels;

public partial class MenuHomeViewModel(NavigatorService navigator) : ViewModelBase
{
	[ObservableProperty] public partial Bitmap? Icon { get; set; } = LoadBitmapFromGodotImage(GameIconPath);

	[RelayCommand]
	private static void OpenGitHubPage() => OS.ShellOpen("https://github.com/SubParSuperCar/Ensemble");

	[RelayCommand]
	private static void StartSession() => GSessionManager.StartSinglePlayer();

	[RelayCommand]
	private void GoToReadMe() => navigator.GoTo<ReadMeViewModel>();

	[RelayCommand]
	private void GoToWebBrowser() => navigator.GoTo<WebBrowserViewModel>();

	private static Bitmap? LoadBitmapFromGodotImage(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);

		if (file is null)
		{
			Log.Warning("Failed to open {Path}: {Error}", path, FileAccess.GetOpenError());
			return null;
		}

		var buffer = file.GetBuffer((long)file.GetLength());

		using var stream = new MemoryStream(buffer);
		return new Bitmap(stream);
	}
}
