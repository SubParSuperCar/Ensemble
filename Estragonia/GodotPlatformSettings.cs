using Avalonia.Platform;
using Godot;

namespace Estragonia;

/// <summary>Implementation of <see cref="IPlatformSettings" /> for Godot.</summary>
internal sealed class GodotPlatformSettings : DefaultPlatformSettings
{
	public override PlatformColorValues GetColorValues() =>
		new()
		{
			ThemeVariant = DisplayServer.IsDarkMode() ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light,
			ContrastPreference = ColorContrastPreference.NoPreference,
			AccentColor1 = DisplayServer.GetAccentColor().ToAvaloniaColor()
		};
}
