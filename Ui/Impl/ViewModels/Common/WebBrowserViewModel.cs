using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.Ui.Impl.Abstractions;
using Serilog;

namespace EnsembleRoot.Ui.Impl.ViewModels;

/// <remarks>
///     <see cref="Source" /> is bound two-way to the web view, so setting it navigates and navigating updates it.
///     The view executes the <see cref="NavigationRequested" /> requests and reports back through
///     <see cref="SyncNavigationState" />.
/// </remarks>
public sealed partial class WebBrowserViewModel : ViewModelBase, IWidget
{
	private const string HomePageUri = "https://www.google.com/";

	private bool _isNavigating;

	[ObservableProperty] public partial Uri Source { get; set; } = new(HomePageUri);

	/// <summary>The address bar text, which follows <see cref="Source" /> unless being edited.</summary>
	[ObservableProperty]
	public partial string Address { get; set; } = HomePageUri;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
	public partial bool CanGoBack { get; private set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(GoForwardCommand))]
	public partial bool CanGoForward { get; private set; }

	public bool IsEditingAddress { get; set; }

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(3, 2, 10, 12))
	{
		Description = "Lets you browse the web without leaving Ensemble.",
		MinSize = new Size(512, 384)
	};

	public event Action<WebNavigation>? NavigationRequested;

	public void SyncNavigationState(bool isNavigating, bool canGoBack, bool canGoForward)
	{
		_isNavigating = isNavigating;
		CanGoBack = canGoBack;
		CanGoForward = canGoForward;

		if (!IsEditingAddress)
			Address = Source.ToString();
	}

	protected override void OnDispose() => NavigationRequested = null;

	[RelayCommand]
	private void Navigate()
	{
		if (Uri.TryCreate(Address, UriKind.Absolute, out var uri))
			Source = uri;
		else
			Log.Warning("Failed to navigate to {Address}: not an absolute URI", Address);
	}

	[RelayCommand(CanExecute = nameof(CanGoBack))]
	private void GoBack() => NavigationRequested?.Invoke(WebNavigation.Back);

	[RelayCommand(CanExecute = nameof(CanGoForward))]
	private void GoForward() => NavigationRequested?.Invoke(WebNavigation.Forward);

	[RelayCommand]
	private void RefreshOrStop() =>
		NavigationRequested?.Invoke(_isNavigating ? WebNavigation.Stop : WebNavigation.Refresh);
}

public enum WebNavigation : byte
{
	Back,
	Forward,
	Refresh,
	Stop
}
