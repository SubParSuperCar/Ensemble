using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using EnsembleRoot.Ui.Impl.ViewModels;
using Serilog;

namespace EnsembleRoot.Ui.Impl.Views;

public sealed partial class WebBrowserView : UserControl, IViewFor<WebBrowserViewModel>
{
	private const double NavigatingSpeedRatio = 2d;

	private const string WpeLibraryName = "libWPEWebKit-2.0.so.1";
	private const string WpeLegacyBackendExport = "webkit_web_view_backend_new";

	// Avalonia's WPE adapter needs WPE WebKit's legacy libwpe backend API, which newer releases (e.g., 2.54) dropped
	private static readonly Lazy<bool> HasWpeLegacyBackend = new(static () =>
		NativeLibrary.TryLoad(WpeLibraryName, out var handle) &&
		NativeLibrary.TryGetExport(handle, WpeLegacyBackendExport, out _));

	private WebBrowserViewModel? _viewModel;

	public WebBrowserView()
	{
		InitializeComponent();
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

	private void OnWebViewEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
	{
		e.EnableDevTools = true;

		switch (e)
		{
			case WindowsWebView2EnvironmentRequestedEventArgs args:
				args.IsInPrivateModeEnabled = true;
				break;

			case AppleWKWebViewEnvironmentRequestedEventArgs args:
				args.NonPersistentDataStore = true;
				break;

			case GtkWebViewEnvironmentRequestedEventArgs args:
				args.EphemeralDataManager = true;
				break;

			case LinuxWpeWebViewEnvironmentRequestedEventArgs args when !HasWpeLegacyBackend.Value:
				Log.Information(
					"{Library} lacks {Export}; falling back to WebKitGTK", WpeLibraryName, WpeLegacyBackendExport);
				args.PreferWebKitGtkInstead = true;
				break;
		}
	}

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
