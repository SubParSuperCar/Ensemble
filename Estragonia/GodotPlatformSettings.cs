using Avalonia.Platform;
using Godot;

namespace Estragonia;

/// <summary>Implementation of <see cref="IPlatformSettings" /> for Godot.</summary>
/// <remarks>Godot calls back when the OS's theme or accent color changes, on the platforms that tell it.</remarks>
internal sealed class GodotPlatformSettings : DefaultPlatformSettings
{
	private PlatformColorValues? _colorValues;

	public GodotPlatformSettings()
	{
		DisplayServer.SetSystemThemeChangeCallback(Callable.From(OnSystemThemeChanged));
	}

	public override PlatformColorValues GetColorValues() => _colorValues ??= ReadColorValues();

	private static PlatformColorValues ReadColorValues() =>
		new()
		{
			ThemeVariant = DisplayServer.IsDarkMode() ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light,
			ContrastPreference = ColorContrastPreference.NoPreference,
			AccentColor1 = DisplayServer.GetAccentColor().ToAvaloniaColor()
		};

	private void OnSystemThemeChanged()
	{
		var colorValues = ReadColorValues();

		if (colorValues == _colorValues)
			return;

		_colorValues = colorValues;
		OnColorValuesChanged(colorValues);
	}
}
