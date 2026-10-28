using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Tooling.Tools;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.ViewModels.Utils;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class AssetSelectorViewModel : ViewModelBase, IWidget
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

		OnAssetIdChanged(Construct.AssetId);
		SelectedItems.CollectionChanged += OnSelectedItemsChanged;
		Construct.AssetIdChanged += OnAssetIdChanged;

		OnLocalPlotChanged(LocalPlot);
		LocalPlotChanged += OnLocalPlotChanged;
	}

	private static ConstructTool Construct => GToolManager.Construct;

	[ObservableProperty] public partial string FilterQuery { get; set; } = string.Empty;
	[ObservableProperty] public partial string TotalQuota { get; private set; } = QuotaFormat.Unknown;

	public ObservableCollection<INodeBase> VisibleItems { get; } = [];
	public ObservableCollection<INodeBase> SelectedItems { get; } = [];

	[ObservableProperty]
	public partial float LinearSnappingIncrement { get; set; } = Construct.SnappingIncrementLinear ?? 0f;

	[ObservableProperty]
	public partial float AngularSnappingIncrement { get; set; } =
		float.RadiansToDegrees(Construct.SnappingIncrementAngularRadians);

	public RotationSpace[] RotationSpaces { get; } = Enum.GetValues<RotationSpace>();
	[ObservableProperty] public partial RotationSpace RotationSpace { get; set; } = Construct.RotationSpace;

	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(0, 21, 7, 11))
	{
		Description = "Lets you search for, select, and modify the placement asset.",
		MinSize = new Size(384, 256)
	};

	protected override void OnDispose()
	{
		SelectedItems.CollectionChanged -= OnSelectedItemsChanged;
		Construct.AssetIdChanged -= OnAssetIdChanged;
		LocalPlotChanged -= OnLocalPlotChanged;

		SetInstances(null);
	}

	partial void OnLinearSnappingIncrementChanging(float value) =>
		Construct.SnappingIncrementLinear = value is 0f ? null : value;

	partial void OnAngularSnappingIncrementChanging(float value) =>
		Construct.SnappingIncrementAngularRadians = float.DegreesToRadians(value);

	partial void OnRotationSpaceChanging(RotationSpace value) => Construct.RotationSpace = value;

	// ReSharper disable once UnusedParameterInPartialMethod
	partial void OnFilterQueryChanged(string value) => Filter();

	[RelayCommand]
	private static void RotateX() => Construct.RotateX();

	[RelayCommand]
	private static void RotateY() => Construct.RotateY();

	[RelayCommand]
	private static void RotateZ() => Construct.RotateZ();

	[RelayCommand]
	private static void ResetRotation() => Construct.ResetRotation();

	private void AddAssetNode(GdAsset asset)
	{
		var category = GAssetManager.GetCategory(asset.Id);
		var node = new AssetNode
		{
			Name = asset.Name,
			Id = asset.Id,
			Description = GAssetManager.GetDescription(asset.Id)
		};

		_nodesByAssetId.Add(asset.Id, node);
		Insert(GetOrCreateFolder(category, false), node);

		// Per-asset limits don't depend on a plot, unlike the total
		UpdateAssetQuota(asset.Id);
	}

	private IList<INodeBase> GetOrCreateFolder(string category, bool isExpanded)
	{
		IList<INodeBase> children = _items;
		var path = string.Empty;

		foreach (var segment in category.Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
			path = path.Length is 0 ? segment : $"{path}/{segment}";

			var folder = children.OfType<FolderNode>()
				.FirstOrDefault(node => string.Equals(node.Name, segment, StringComparison.Ordinal));

			if (folder is null)
			{
				folder = new FolderNode { Name = segment, Path = path };
				folder.PropertyChanged += OnFolderPropertyChanged;

				folder.IsExpanded = isExpanded || ExpandedFolderPaths.Contains(path);
				Insert(children, folder);
			}
			else if (isExpanded)
				folder.IsExpanded = true;

			children = folder.Children;
		}

		return children;
	}

	private static void Insert(IList<INodeBase> nodes, INodeBase node) =>
		nodes.Insert(nodes.TakeWhile(other => Precedes(other, node)).Count(), node);

	private static bool Precedes(INodeBase left, INodeBase right)
	{
		if (left is FolderNode != right is FolderNode)
			return left is FolderNode;

		return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) switch
		{
			0 => string.CompareOrdinal(left.Name, right.Name) < 0,
			var order => order < 0
		};
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

	// The asset also changes without the selector (e.g., from Lua)
	private void OnAssetIdChanged(int assetId)
	{
		if (SelectedItems.SingleOrDefault() is AssetNode selected && selected.Id == assetId)
			return;

		SelectedItems.Clear();

		if (!_nodesByAssetId.TryGetValue(assetId, out var node))
			return;

		GetOrCreateFolder(GAssetManager.GetCategory(assetId), true);
		SelectedItems.Add(node);
	}

	private void Filter()
	{
		var query = FilterQuery.Trim();

		VisibleItems.Clear();

		foreach (var item in _items)
			if (FilterNode(item, query) is { } filtered)
				VisibleItems.Add(filtered);
	}

	private static INodeBase? FilterNode(INodeBase node, string query)
	{
		if (query.Length is 0)
			return node;

		if (node is AssetNode asset)
			return Matches(asset, query) ? asset : null;

		if (node is not FolderNode folder)
			return null;

		var result = new FolderNode { Name = folder.Name, Path = folder.Path, IsExpanded = true };

		foreach (var child in folder.Children)
			if (FilterNode(child, query) is { } filteredChild)
				result.Children.Add(filteredChild);

		return result.Children.Count is 0 ? null : result;
	}

	private static bool Matches(AssetNode asset, string query) =>
		asset.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
		asset.Id.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.Ordinal);

	private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		switch (SelectedItems.SingleOrDefault())
		{
			case AssetNode asset:
				Construct.SetAsset(asset.Id);
				break;

			case FolderNode folder:
				Dispatcher.UIThread.Post(() => SelectedItems.Remove(folder));
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
			: QuotaFormat.Unknown;

	private void UpdateAssetQuota(int assetId)
	{
		if (!_nodesByAssetId.TryGetValue(assetId, out var node))
			return;

		var (count, maxCount) =
			_instances?.Source.GetQuota(assetId) ?? (0, GAssets.GetAsset(assetId)?.MaxInstanceCount ?? 0);

		node.Quota = QuotaFormat.Fraction(count, maxCount);
	}
}

public sealed partial class FolderNode : ObservableObject, INodeBase
{
	public required string Path { get; init; }
	public IList<INodeBase> Children { get; init; } = [];

	[ObservableProperty] public partial bool IsExpanded { get; set; }
	public required string Name { get; init; }
}

public sealed partial class AssetNode : ObservableObject, INodeBase
{
	public int Id { get; init; }
	public string? Description { get; init; }

	[ObservableProperty] public partial string Quota { get; internal set; } = QuotaFormat.Unknown;
	public required string Name { get; init; }
}

public interface INodeBase
{
	string Name { get; }
}
