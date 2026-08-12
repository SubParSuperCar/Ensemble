using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using Root.Ui.Impl.Messages;

namespace Root.Ui.Impl;

public class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

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

	private void OnColorValuesChanged(PlatformColorValues colorValues) =>
		RequestedThemeVariant = colorValues.ThemeVariant switch
		{
			PlatformThemeVariant.Light => ThemeVariant.Light,
			PlatformThemeVariant.Dark => ThemeVariant.Dark,
			_ => ThemeVariant.Default
		};
}
