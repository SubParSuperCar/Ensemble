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
using EnsembleRoot.Sessions;
using EnsembleRoot.Ui.Impl.Extensions;
using Iciclecreek.Terminal;
using LiveMarkdown.Avalonia;
using Serilog;

namespace EnsembleRoot.Ui.Impl;

public sealed class App : Application
{
	private static readonly object FocusSinkToken = new();

	private static bool IsInSession => SessionManager.Instance?.IsActive is true;

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

		WeakReferenceMessenger.Default.Register<App, SetUiThemeMessage>(this,
			static (app, message) => app.RequestedThemeVariant = message.Value switch
			{
				true => ThemeVariant.Dark,
				false => ThemeVariant.Light,
				_ => ThemeVariant.Default
			});

		base.OnFrameworkInitializationCompleted();

#if ENSEMBLE_DEBUG
		try
		{
			// F12 opens the Avalonia developer tools
			this.AttachDeveloperTools(static options => options.Gesture = KeyGesture.Parse("F12"));
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to attach Avalonia developer tools");
		}
#endif

		ApplyAccentColor();
	}

	// Sinks game input while a text input has focus
	private static void OnFocusChanged((object Sender, RoutedEventArgs Args) value)
	{
		if (value.Args is not FocusChangedEventArgs focus)
			return;

		if (focus.NewFocusedElement is TextBox or TextArea or TerminalView)
			InputSink.Sink.Acquire(FocusSinkToken);
		else
			InputSink.Sink.Release(FocusSinkToken);
	}

	// Swallows Space and Tab, and all navigation keys while in session, to prevent unintended UI navigation
	private static void OnKeyDownOrUp(TopLevel topLevel, KeyEventArgs args)
	{
		if (InputSink.IsSunk)
			return;

		if (args.Key is Key.Space or Key.Tab || (IsInSession && IsNavigationKey(args.Key)))
			args.Handled = true;
	}

	private static bool IsNavigationKey(Key key) =>
		key is Key.Up or Key.Down or Key.Left or Key.Right or Key.PageUp or Key.PageDown or Key.Home or Key.End
			or Key.Enter;

	// Applied once at startup, since Estragonia doesn't poll for changes. The accent is made more vibrant in OKLCH so
	// washed-out system colors (e.g., brown) still stand out.
	private void ApplyAccentColor()
	{
		if (PlatformSettings?.GetColorValues().AccentColor1 is not { A: > 0 } accent)
			return;

		Resources.ThemeDictionaries[ThemeVariant.Default] = CreateAccentResources(accent.ToReadable(false));
		Resources.ThemeDictionaries[ThemeVariant.Dark] = CreateAccentResources(accent.ToReadable(true));
	}

	// Mirrors SimpleTheme's own accent opacity ramp
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
