using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Ui.Impl.Extensions;
using EnsembleRoot.Ui.Impl.Messages;
using LiveMarkdown.Avalonia;

namespace EnsembleRoot.Ui.Impl;

public class App : Application
{
	// OKLCH lightness bounds keeping the accent >= 3:1 against SimpleTheme's background and its foreground >= 4.5:1
	private const double LightAccentMinLightness = 0.45;
	private const double LightAccentMaxLightness = 0.6;
	private const double DarkAccentMinLightness = 0.62;
	private const double DarkAccentMaxLightness = 0.78;

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

		ApplyAccentColor();
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

	private void ApplyAccentColor()
	{
		if (PlatformSettings?.GetColorValues().AccentColor1 is not { A: > 0 } accent)
			return;

		Resources.ThemeDictionaries[ThemeVariant.Default] =
			CreateAccentResources(accent.ToVibrant(LightAccentMinLightness, LightAccentMaxLightness));
		Resources.ThemeDictionaries[ThemeVariant.Dark] =
			CreateAccentResources(accent.ToVibrant(DarkAccentMinLightness, DarkAccentMaxLightness));
	}

	private static ResourceDictionary CreateAccentResources(Color accent) =>
		new()
		{
			["HighlightBrush"] = new SolidColorBrush(accent),
			["HighlightForegroundBrush"] = new SolidColorBrush(accent.ContrastingForeground),
			["ThemeAccentBrush"] = new SolidColorBrush(accent, 0.8),
			["ThemeAccentBrush2"] = new SolidColorBrush(accent, 0.6),
			["ThemeAccentBrush3"] = new SolidColorBrush(accent, 0.4),
			["ThemeAccentBrush4"] = new SolidColorBrush(accent, 0.2)
		};
}
