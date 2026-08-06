using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;
using Serilog;
using InputExtensions = Root.Common.Input.InputExtensions;
using Key = Avalonia.Input.Key;

namespace Root.Ui.Impl.Views;

public partial class WebBrowserView : UserControl, IViewFor<WebBrowserViewModel>
{
	private readonly BindingExpressionBase? _urlBoxBinding;
	private bool _isNavigating;

	public WebBrowserView()
	{
		InitializeComponent();

		_urlBoxBinding = BindingOperations.GetBindingExpressionBase(UrlBox, TextBox.TextProperty);
	}

	[RelayCommand(CanExecute = nameof(CanGoBack))]
	private void GoBack() => WebView.GoBack();

	[RelayCommand(CanExecute = nameof(CanGoForward))]
	private void GoForward() => WebView.GoForward();

	private bool CanGoBack() => WebView.CanGoBack;
	private bool CanGoForward() => WebView.CanGoForward;

	[RelayCommand]
	private void RefreshOrStop()
	{
		if (_isNavigating)
			WebView.Stop();
		else
			WebView.Refresh();
	}

	private void UrlBox_OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is Key.Enter)
			_urlBoxBinding?.UpdateSource();
	}

	private void Control_GotFocus(object? sender, FocusChangedEventArgs e) => InputExtensions.Sink.Acquire(this);
	private void Control_LostFocus(object? sender, FocusChangedEventArgs e) => InputExtensions.Sink.Release(this);

	private void OnNavigationStateChanged()
	{
		if (!UrlBox.IsFocused)
			_urlBoxBinding?.UpdateTarget();

		GoBackCommand.NotifyCanExecuteChanged();
		GoForwardCommand.NotifyCanExecuteChanged();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (InputExtensions.Sink.IsHeldBy(this))
			InputExtensions.Sink.Release(this);

		base.OnDetachedFromVisualTree(e);
	}

	private void WebView_OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
	{
		OnNavigationStateChanged();

		_isNavigating = true;
		LoadingIndicator.SpeedRatio = 2;
	}

	private void WebView_OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
	{
		OnNavigationStateChanged();

		_isNavigating = false;

		LoadingIndicator.IsActive = false;
		LoadingIndicator.SpeedRatio = 0;
		LoadingIndicator.IsActive = true;
	}

	private void WebView_OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
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
			/* TODO: case LinuxWpeWebViewEnvironmentRequestedEventArgs args:
				args.PreferWebKitGtkInstead = true;
				break; */
		}
	}

	private void WebView_OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
	{
		if (WebView.AdapterInfo is { } info)
			Log.Debug(
				"WebView adapter created: Engine={Engine}, Type={Type}, Version={Version}",
				info.Engine,
				info.Type,
				info.Version);
	}

	private void WebView_OnAdapterDestroyed(object? sender, WebViewAdapterEventArgs e) =>
		Log.Debug("WebView adapter destroyed");
}
