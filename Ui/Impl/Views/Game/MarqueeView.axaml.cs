using System.ComponentModel;
using Avalonia.Animation;
using Avalonia.Controls;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels;

namespace EnsembleRoot.Ui.Views;

public sealed partial class MarqueeView : UserControl, IViewFor<MarqueeViewModel>
{
	// Shown at once, faded out when done
	private readonly Transitions _fadeOut =
		[new DoubleTransition { Property = OpacityProperty, Duration = AnimationDuration }];

	private MarqueeViewModel? _viewModel;

	public MarqueeView()
	{
		InitializeComponent();
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		_viewModel?.PropertyChanged -= OnViewModelPropertyChanged;
		_viewModel = DataContext as MarqueeViewModel;

		UpdateArea();
		UpdateIsActive();
		_viewModel?.PropertyChanged += OnViewModelPropertyChanged;
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(MarqueeViewModel.Area):
				UpdateArea();
				break;

			case nameof(MarqueeViewModel.IsActive):
				UpdateIsActive();
				break;
		}
	}

	// The area is in viewport pixels
	private void UpdateArea()
	{
		if (_viewModel is null || TopLevel.GetTopLevel(this) is not { RenderScaling: var scaling })
			return;

		var area = _viewModel.Area;

		Canvas.SetLeft(Box, area.X / scaling);
		Canvas.SetTop(Box, area.Y / scaling);

		Box.Width = area.Width / scaling;
		Box.Height = area.Height / scaling;
	}

	private void UpdateIsActive()
	{
		var isActive = _viewModel?.IsActive is true;

		Box.Transitions = isActive ? null : _fadeOut;
		Box.Opacity = isActive ? 1 : 0;
	}
}
