using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Root.Ui.Impl;

public class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	// TODO
	public override void OnFrameworkInitializationCompleted()
	{
		if (RequestedThemeVariant == ThemeVariant.Default && PlatformSettings is not null)
		{
			UpdateTheme(PlatformSettings.GetColorValues());
			PlatformSettings.ColorValuesChanged += (_, colorValues) => UpdateTheme(colorValues);
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void UpdateTheme(PlatformColorValues colorValues) =>
		RequestedThemeVariant = colorValues.ThemeVariant switch
		{
			PlatformThemeVariant.Light => ThemeVariant.Light,
			PlatformThemeVariant.Dark => ThemeVariant.Dark,
			_ => ThemeVariant.Default
		};
}
