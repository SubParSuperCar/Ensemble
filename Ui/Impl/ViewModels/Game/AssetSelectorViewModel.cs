using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Godot;
using Root.GdCore.Assets;
using Root.GdCore.Plots;
using Root.Tooling.Tools;
using Root.Ui.Impl.Abstractions;

namespace Root.Ui.Impl.ViewModels;

public partial class AssetSelectorViewModel : ViewModelBase
{
	private static readonly HashSet<string> ExpandedFolderPaths = [];

	private readonly List<INodeBase> _items = [];
	private readonly Dictionary<int, AssetNode> _nodesByAssetId = [];
	private GdInstances? _instances;

	public AssetSelectorViewModel()
	{
		foreach (var asset in GAssets.GetAll())
			AddAssetNode(asset);

		Filter();

		SelectedItems.CollectionChanged += OnSelectedItemsChanged;
		SelectInitialAsset();

		LocalPlotChanged += OnLocalPlotChanged;
		OnLocalPlotChanged(LocalPlot);
	}

	private static ConstructTool Ctor => GToolManager.Construct;

	[ObservableProperty] public partial string FilterQuery { get; set; } = string.Empty;
	[ObservableProperty] public partial string TotalQuota { get; set; } = "<Unknown>";

	public ObservableCollection<INodeBase> VisibleItems { get; } = [];
	public ObservableCollection<INodeBase> SelectedItems { get; } = [];

	[ObservableProperty] public partial float LinearSnappingIncrement { get; set; } = Ctor.SnappingIncrementLinear ?? 0;

	[ObservableProperty]
	public partial float AngularSnappingIncrement { get; set; } = Mathf.RadToDeg(Ctor.SnappingIncrementAngularRadians);

	public RotationSpace[] RotationSpaces { get; } = Enum.GetValues<RotationSpace>();
	[ObservableProperty] public partial RotationSpace RotationSpace { get; set; } = Ctor.RotationSpace;

	protected override void OnDispose()
	{
		SelectedItems.CollectionChanged -= OnSelectedItemsChanged;
		LocalPlotChanged -= OnLocalPlotChanged;

		SetInstances(null);
	}

	partial void OnLinearSnappingIncrementChanging(float value) =>
		Ctor.SnappingIncrementLinear = value is 0 ? null : value;

	partial void OnAngularSnappingIncrementChanging(float value) =>
		Ctor.SnappingIncrementAngularRadians = Mathf.DegToRad(value);

	partial void OnRotationSpaceChanging(RotationSpace value) => Ctor.RotationSpace = value;

	// ReSharper disable once UnusedParameterInPartialMethod
	partial void OnFilterQueryChanged(string value) => Filter();

	[RelayCommand]
	private static void RotateX() => Ctor.RotateX();

	[RelayCommand]
	private static void RotateY() => Ctor.RotateY();

	[RelayCommand]
	private static void RotateZ() => Ctor.RotateZ();

	[RelayCommand]
	private static void ResetRotation() => Ctor.ResetRotation();

	private void AddAssetNode(GdAsset asset)
	{
		var category = GAssetManager.Categories.TryGetValue(asset.Id, out var path) ? path : string.Empty;
		var node = new AssetNode { Name = asset.Name, Id = asset.Id };

		_nodesByAssetId[asset.Id] = node;
		GetOrCreateFolder(category, asset.Id == Ctor.AssetId).Add(node);
	}

	private IList<INodeBase> GetOrCreateFolder(string category, bool expand)
	{
		IList<INodeBase> children = _items;
		var path = string.Empty;

		foreach (var segment in category.Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
			path = path.Length is 0 ? segment : $"{path}/{segment}";

			var folder = children.OfType<FolderNode>()
				.FirstOrDefault(f => string.Equals(f.Name, segment, StringComparison.Ordinal));

			if (folder is null)
			{
				folder = new FolderNode
				{
					Name = segment,
					Path = path,
					IsExpanded = expand || ExpandedFolderPaths.Contains(path)
				};

				folder.PropertyChanged += OnFolderPropertyChanged;
				children.Add(folder);
			}
			else if (expand)
				folder.IsExpanded = true;

			children = folder.Children;
		}

		return children;
	}

	private static void OnFolderPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is not nameof(FolderNode.IsExpanded) || sender is not FolderNode folder)
			return;

		if (folder.IsExpanded)
			ExpandedFolderPaths.Add(folder.Path);
		else
			ExpandedFolderPaths.Remove(folder.Path);
	}

	private void SelectInitialAsset()
	{
		if (_nodesByAssetId.TryGetValue(Ctor.AssetId, out var node))
			SelectedItems.Add(node);
	}

	private void Filter()
	{
		VisibleItems.Clear();

		foreach (var item in _items)
			if (FilterNode(item) is { } filtered)
				VisibleItems.Add(filtered);
	}

	private INodeBase? FilterNode(INodeBase node)
	{
		if (string.IsNullOrWhiteSpace(FilterQuery))
			return node;

		if (node is AssetNode asset)
			return Matches(asset) ? asset : null;

		if (node is not FolderNode folder)
			return null;

		var result = new FolderNode { Name = folder.Name, Path = folder.Path, IsExpanded = true };

		foreach (var child in folder.Children)
			if (FilterNode(child) is { } filteredChild)
				result.Children.Add(filteredChild);

		return result.Children.Count > 0 ? result : null;
	}

	private bool Matches(AssetNode asset) =>
		asset.Name.Contains(FilterQuery, StringComparison.OrdinalIgnoreCase) ||
		asset.Id.ToString(CultureInfo.InvariantCulture).Contains(FilterQuery, StringComparison.OrdinalIgnoreCase);

	private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		switch (SelectedItems.SingleOrDefault())
		{
			case AssetNode asset:
				Ctor.SetAsset(asset.Id);
				break;

			case FolderNode:
				SelectedItems.Clear();
				break;
		}
	}

	private void OnLocalPlotChanged(GdPlot? plot) => SetInstances(plot?.Instances);

	private void SetInstances(GdInstances? instances)
	{
		if (ReferenceEquals(_instances, instances))
			return;

		if (_instances is not null)
		{
			_instances.Added -= OnInstanceChanged;
			_instances.Removed -= OnInstanceChanged;
		}

		_instances = instances;

		if (instances is not null)
		{
			instances.Added += OnInstanceChanged;
			instances.Removed += OnInstanceChanged;
		}

		UpdateTotalQuota();

		foreach (var assetId in _nodesByAssetId.Keys)
			UpdateAssetQuota(assetId);
	}

	private void OnInstanceChanged(GdInstance instance)
	{
		UpdateTotalQuota();
		UpdateAssetQuota(instance.Asset.Id);
	}

	private void UpdateTotalQuota() =>
		TotalQuota = _instances is { } instances
			? QuotaFormat.Fraction(instances.Count, instances.MaxCount)
			: "<Unknown>";

	private void UpdateAssetQuota(int assetId)
	{
		if (!_nodesByAssetId.TryGetValue(assetId, out var node))
			return;

		var counts = _instances?.GetCount(assetId);
		var count = counts?[0] ?? 0;
		var max = counts?[1] ?? GAssets.GetAsset(assetId)?.MaxInstanceCount ?? 0;

		node.Quota = QuotaFormat.Fraction(count, max);
	}
}

public partial class FolderNode : ObservableObject, INodeBase
{
	public required string Name { get; init; }
	public required string Path { get; init; }
	public IList<INodeBase> Children { get; init; } = new List<INodeBase>();

	[ObservableProperty] public partial bool IsExpanded { get; set; }
}

public partial class AssetNode : ObservableObject, INodeBase
{
	public required string Name { get; init; }
	public int Id { get; init; }

	[ObservableProperty] public partial string Quota { get; set; } = "???";
}

public interface INodeBase;
