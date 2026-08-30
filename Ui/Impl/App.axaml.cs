using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using Root.Common.Input;
using Root.Ui.Impl.Messages;

namespace Root.Ui.Impl;

public class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		var registry = AvaloniaPropertyRegistry.Instance;
		var tooltipStyle = new Style(x => x.OfType<ToolTip>());

		if (registry.FindRegistered(typeof(TextOptions), "TextRenderingMode") is { } property)
			tooltipStyle.Setters.Add(new Setter(property, TextRenderingMode.Antialias));

		Styles.Add(tooltipStyle);

		InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDownOrUp, RoutingStrategies.Tunnel);
		InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnKeyDownOrUp, RoutingStrategies.Tunnel);

		WeakReferenceMessenger.Default.Register<SetUiThemeMessage>(this,
			(_, message) => RequestedThemeVariant = message.Value);

		base.OnFrameworkInitializationCompleted();
	}

	private static void OnKeyDownOrUp(TopLevel topLevel, KeyEventArgs e)
	{
		if (e.Key is Key.Space && !InputSink.IsSunk)
			e.Handled = true;
	}
}
