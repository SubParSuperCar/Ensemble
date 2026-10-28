using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;
using Serilog;

namespace EnsembleRoot.Ui.Views;

public sealed partial class WebBrowserView : UserControl, IViewFor<WebBrowserViewModel>
{
	private const double NavigatingSpeedRatio = 2d;

	private const string WpeLibraryName = "libWPEWebKit-2.0.so.1";
	private const string WpeLegacyBackendExport = "webkit_web_view_backend_new";

	// Estragonia has no native window to host web views in, so only WPE WebKit's offscreen adapter can render. That
	// adapter (as of Avalonia 12.1.1) also needs WPE WebKit's legacy libwpe backend API, which 2.54 dropped.
	private static readonly Lazy<bool> IsSupported = new(static () =>
		OperatingSystem.IsLinux() &&
		NativeLibrary.TryLoad(WpeLibraryName, out var handle) &&
		NativeLibrary.TryGetExport(handle, WpeLegacyBackendExport, out _));

	private WebBrowserViewModel? _viewModel;

	public WebBrowserView()
	{
		InitializeComponent();

		if (IsSupported.Value)
		{
			WebView.AdapterPreference = [WebViewAdapterType.WpeWebKit];
			return;
		}

		// Never attached, it never starts an adapter, which leaves the explanation beneath it showing
		Log.Information(
			"Web view unsupported: needs Linux and {Library} with {Export}", WpeLibraryName, WpeLegacyBackendExport);
		((Panel)WebView.Parent!).Children.Remove(WebView);
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		_viewModel?.NavigationRequested -= OnNavigationRequested;
		_viewModel = DataContext as WebBrowserViewModel;
		_viewModel?.NavigationRequested += OnNavigationRequested;

		base.OnDataContextChanged(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		InputSink.Sink.Release(this);
		base.OnDetachedFromVisualTree(e);
	}

	private void OnNavigationRequested(WebNavigation navigation) =>
		_ = navigation switch
		{
			WebNavigation.Back => WebView.GoBack(),
			WebNavigation.Forward => WebView.GoForward(),
			WebNavigation.Refresh => WebView.Refresh(),
			WebNavigation.Stop => WebView.Stop(),
			_ => throw new ArgumentOutOfRangeException(nameof(navigation), navigation, null)
		};

	private void OnAddressBoxGotFocus(object? sender, FocusChangedEventArgs e) => _viewModel?.IsEditingAddress = true;
	private void OnAddressBoxLostFocus(object? sender, FocusChangedEventArgs e) => _viewModel?.IsEditingAddress = false;

	private void OnWebViewGotFocus(object? sender, FocusChangedEventArgs e) => InputSink.Sink.Acquire(this);
	private void OnWebViewLostFocus(object? sender, FocusChangedEventArgs e) => InputSink.Sink.Release(this);

	private void OnWebViewNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
	{
		SyncNavigationState(true);
		LoadingIndicator.SpeedRatio = NavigatingSpeedRatio;
	}

	// Toggling IsActive restarts the indicator, which rests when its speed is zero
	private void OnWebViewNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
	{
		SyncNavigationState(false);

		LoadingIndicator.IsActive = false;
		LoadingIndicator.SpeedRatio = 0;
		LoadingIndicator.IsActive = true;
	}

	private void SyncNavigationState(bool isNavigating) =>
		_viewModel?.SyncNavigationState(isNavigating, WebView.CanGoBack, WebView.CanGoForward);

	private void OnWebViewEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e) =>
		e.EnableDevTools = true;

	private void OnWebViewAdapterCreated(object? sender, WebViewAdapterEventArgs e)
	{
		if (WebView.AdapterInfo is not { } info)
			return;

		Log.Debug(
			"WebView adapter created (Engine={Engine}, Type={Type}, Version={Version})",
			info.Engine, info.Type, info.Version);
	}

	private void OnWebViewAdapterDestroyed(object? sender, WebViewAdapterEventArgs e) =>
		Log.Debug("WebView adapter destroyed");
}
