using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Controls;
using EnsembleRoot.Ui.ViewModels;
using Godot;

namespace EnsembleRoot.Ui.Views;

public sealed partial class AssetSelectorView : UserControl, IViewFor<AssetSelectorViewModel>
{
	private const string FolderClass = "folder";

	private readonly PressFlash _rotateXFlash;
	private readonly PressFlash _rotateYFlash;
	private readonly PressFlash _rotateZFlash;

	private AssetSelectorViewModel? _viewModel;

	public AssetSelectorView()
	{
		InitializeComponent();

		_rotateXFlash = new PressFlash(RotateXButton);
		_rotateYFlash = new PressFlash(RotateYButton);
		_rotateZFlash = new PressFlash(RotateZButton);

		// Tunneling, so the reset preempts the text box's own context menu
		LinearIncrementBox.AddHandler(
			ContextRequestedEvent, OnLinearIncrementContextRequested, RoutingStrategies.Tunnel);
		AngularIncrementBox.AddHandler(
			ContextRequestedEvent, OnAngularIncrementContextRequested, RoutingStrategies.Tunnel);
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		base.OnDataContextChanged(e);

		_viewModel?.Rotated -= OnRotated;
		_viewModel = DataContext as AssetSelectorViewModel;
		_viewModel?.Rotated += OnRotated;
	}

	private void OnRotated(Vector3.Axis axis)
	{
		var flash = axis switch
		{
			Vector3.Axis.X => _rotateXFlash,
			Vector3.Axis.Y => _rotateYFlash,
			_ => _rotateZFlash
		};

		flash.Flash();
	}

	private void OnLinearIncrementContextRequested(object? sender, ContextRequestedEventArgs e)
	{
		_viewModel?.ResetLinearSnappingIncrement();
		e.Handled = true;
	}

	private void OnAngularIncrementContextRequested(object? sender, ContextRequestedEventArgs e)
	{
		_viewModel?.ResetAngularSnappingIncrement();
		e.Handled = true;
	}

	private void OnFilterBoxGotFocus(object? sender, FocusChangedEventArgs e) => _viewModel?.FilterQuery = string.Empty;

	private void OnTreeViewContainerPrepared(object? sender, ContainerPreparedEventArgs e)
	{
		if (sender is not ItemsControl owner || e.Container is not TreeViewItem item)
			return;

		item.Classes.Set(FolderClass, owner.ItemFromContainer(item) is FolderNode);

		item.ContainerPrepared -= OnTreeViewContainerPrepared;
		item.ContainerPrepared += OnTreeViewContainerPrepared;
	}
}
