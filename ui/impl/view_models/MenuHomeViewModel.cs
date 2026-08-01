using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Godot;
using Root.Common.Globals;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.Services;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using FileAccess = Godot.FileAccess;

namespace Root.Ui.Impl.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MenuHomeViewModel(NavigatorService navigator) : ViewModelBase
{
	[ObservableProperty]
	public partial Bitmap? Icon { get; set; } = LoadBitmapFromGodotImage(CommonConstants.GameIconPath);

	[RelayCommand]
	private static void StartSession() => GSessionManager.StartSinglePlayer();

	[RelayCommand]
	private void GoToReadMe() => navigator.GoTo<ReadMeViewModel>();

	[RelayCommand]
	private void GoToWebBrowser() => navigator.GoTo<WebBrowserViewModel>();

	[RelayCommand]
	private static void ShutDown() => (Engine.GetMainLoop() as SceneTree)?.Quit();

	private static Bitmap LoadBitmapFromGodotImage(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var buffer = file.GetBuffer((long)file.GetLength());

		using var stream = new MemoryStream(buffer);
		return new Bitmap(stream);
	}
}
