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
	}

	// Sink/mark certain keystrokes as handled to prevent unintentional UI navigation
	private static void OnKeyDownOrUp(TopLevel topLevel, KeyEventArgs e)
	{
		if (e.Key is Key.Space or Key.Tab && !InputSink.IsSunk)
			e.Handled = true;
	}
}
