using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;
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

	private void OnNavigationStateChanged(object? sender, object e)
	{
		if (!UrlBox.IsFocused)
			_urlBoxBinding?.UpdateTarget();

		GoBackCommand.NotifyCanExecuteChanged();
		GoForwardCommand.NotifyCanExecuteChanged();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		WebView.NavigationStarted += OnNavigationStateChanged;
		WebView.NavigationCompleted += OnNavigationStateChanged;
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		WebView.NavigationStarted -= OnNavigationStateChanged;
		WebView.NavigationCompleted -= OnNavigationStateChanged;

		if (InputExtensions.Sink.IsHeldBy(this))
			InputExtensions.Sink.Release(this);

		base.OnDetachedFromVisualTree(e);
	}

	private void WebView_OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
	{
		_isNavigating = true;
		LoadingIndicator.SpeedRatio = 2;
	}

	private void WebView_OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
	{
		_isNavigating = false;

		LoadingIndicator.IsActive = false;
		LoadingIndicator.SpeedRatio = 0;
		LoadingIndicator.IsActive = true;
	}
}
