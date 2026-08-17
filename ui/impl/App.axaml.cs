using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using Root.Ui.Impl.Messages;
using InputExtensions = Root.Common.Input.InputExtensions;

namespace Root.Ui.Impl;

public class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);

		InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDownOrUp, RoutingStrategies.Tunnel);
		InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnKeyDownOrUp, RoutingStrategies.Tunnel);
	}

	// TODO
	public override void OnFrameworkInitializationCompleted()
	{
		if (RequestedThemeVariant == ThemeVariant.Default && PlatformSettings is not null)
		{
			OnColorValuesChanged(PlatformSettings.GetColorValues());
			PlatformSettings.ColorValuesChanged += (_, colorValues) => OnColorValuesChanged(colorValues);
		}

		WeakReferenceMessenger.Default.Register<SetThemeMessage>(this,
			(_, message) => RequestedThemeVariant = message.Value);

		base.OnFrameworkInitializationCompleted();
	}

	// TODO
	private static void OnKeyDownOrUp(TopLevel topLevel, KeyEventArgs e)
	{
		if (e.Key is Key.Space && topLevel.FocusManager.GetFocusedElement() is not TextBox && !InputExtensions.IsSunk)
			e.Handled = true;
	}

	private void OnColorValuesChanged(PlatformColorValues colorValues) =>
		RequestedThemeVariant = colorValues.ThemeVariant switch
		{
			PlatformThemeVariant.Light => ThemeVariant.Light,
			PlatformThemeVariant.Dark => ThemeVariant.Dark,
			_ => ThemeVariant.Default
		};
}
