using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Root.Ui.Impl.Abstractions;
using Root.Ui.Impl.ViewModels;
using InputExtensions = Root.Common.Input.InputExtensions;

namespace Root.Ui.Impl.Views;

// ReSharper disable once UnusedType.Global
public partial class WebBrowserView : UserControl, IViewFor<WebBrowserViewModel>
{
	private readonly BindingExpressionBase? _urlBoxBinding;

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

	private void UrlBox_OnKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key is Key.Enter)
			_urlBoxBinding?.UpdateSource();
	}

	private void UrlBox_GotFocus(object? sender, FocusChangedEventArgs e) => InputExtensions.Sink.Acquire(this);
	private void UrlBox_LostFocus(object? sender, FocusChangedEventArgs e) => InputExtensions.Sink.Release(this);

	private void OnNavigationStateChanged(object? sender, object e)
	{
		GoBackCommand.NotifyCanExecuteChanged();
		GoForwardCommand.NotifyCanExecuteChanged();

		if (!UrlBox.IsFocused)
			_urlBoxBinding?.UpdateTarget();
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
}
