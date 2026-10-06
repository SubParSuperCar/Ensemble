using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Styling;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Common.Messages;
using EnsembleRoot.Ui.Impl.Extensions;
using Iciclecreek.Terminal;
using LiveMarkdown.Avalonia;
using Serilog;

namespace EnsembleRoot.Ui.Impl;

public class App : Application
{
	// OKLCH lightness bounds keeping the accent >= 3:1 against SimpleTheme's background and its foreground >= 4.5:1
	private const double LightAccentMinLightness = 0.45;
	private const double LightAccentMaxLightness = 0.6;
	private const double DarkAccentMinLightness = 0.62;
	private const double DarkAccentMaxLightness = 0.78;

	private static readonly object FocusSinkToken = new();

	private static bool IsInSession => SessionManager.SessionManager.Instance?.IsActive is true;

	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		AsyncImageLoader.DefaultDecoders = [SvgImageDecoder.Shared, DefaultBitmapDecoder.Shared];
		MarkdownRenderer.ConfigurePipeline += static pipeline => pipeline.UseMermaid();
		MarkdownNode.Register<MermaidBlockNode>();

		InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDownOrUp, RoutingStrategies.Tunnel);
		InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnKeyDownOrUp, RoutingStrategies.Tunnel);

		var focusObserver = new AnonymousObserver<(object, RoutedEventArgs)>(OnFocusChanged);
		InputElement.GotFocusEvent.Raised.Subscribe(focusObserver);
		InputElement.LostFocusEvent.Raised.Subscribe(focusObserver);

		WeakReferenceMessenger.Default.Register<SetUiThemeMessage>(this,
			(_, message) => RequestedThemeVariant = message.Value switch
			{
				true => ThemeVariant.Dark,
				false => ThemeVariant.Light,
				_ => ThemeVariant.Default
			});

		base.OnFrameworkInitializationCompleted();

#if ENSEMBLE_DEBUG
		try
		{
			// Press F12 to open the Avalonia developer tools (pin keybind gesture)
			this.AttachDeveloperTools(static options => options.Gesture = KeyGesture.Parse("F12"));
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to attach Avalonia developer tools");
		}
#endif

		ApplyAccentColor();
	}

	// Sink game input while a text input has focus
	private static void OnFocusChanged((object Sender, RoutedEventArgs Args) value)
	{
		if (value.Args is not FocusChangedEventArgs focus)
			return;

		if (focus.NewFocusedElement is TextBox or TextArea or TerminalView)
			InputSink.Sink.Acquire(FocusSinkToken);
		else
			InputSink.Sink.Release(FocusSinkToken);
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
		// Set the UI accent color for the 'Simple' theme once at startup, because Estragonia doesn't poll for changes
		// Also boost vibrancy using an advanced color algorithm to make washed-out colors like brown pop out
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
