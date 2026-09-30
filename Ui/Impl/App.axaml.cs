using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Ui.Impl.Messages;
using LiveMarkdown.Avalonia;

namespace EnsembleRoot.Ui.Impl;

public class App : Application
{
	private static bool IsInSession => SessionManager.SessionManager.Instance?.IsActive is true;

	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		AsyncImageLoader.DefaultDecoders =
		[
			SvgImageDecoder.Shared,
			DefaultBitmapDecoder.Shared
		];

		InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDownOrUp, RoutingStrategies.Tunnel);
		InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnKeyDownOrUp, RoutingStrategies.Tunnel);

		WeakReferenceMessenger.Default.Register<SetUiThemeMessage>(this,
			(_, message) => RequestedThemeVariant = message.Value);

		base.OnFrameworkInitializationCompleted();

#if ENSEMBLE_DEBUG
		this.AttachDeveloperTools();
#endif
	}

	// Sink/mark keystrokes as handled to prevent unintentional UI navigation, and all navigation keys while in-session
	private static void OnKeyDownOrUp(TopLevel topLevel, KeyEventArgs e)
	{
		if (InputSink.IsSunk)
			return;

		if (
			e.Key is Key.Space or Key.Tab ||
			(IsInSession &&
			 e.Key is Key.Up or Key.Down or Key.Left or Key.Right
				 or Key.PageUp or Key.PageDown or Key.Home or Key.End or Key.Enter))
			e.Handled = true;
	}
}
