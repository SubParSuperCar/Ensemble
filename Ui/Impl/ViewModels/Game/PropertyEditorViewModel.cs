using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Tooling.Tools;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Extensions;

namespace EnsembleRoot.Ui.ViewModels;

public sealed partial class PropertyEditorViewModel : ViewModelBase, IWidget
{
	private readonly Dictionary<PropertyKey, PropertyItem> _itemsByKey = [];

	public PropertyEditorViewModel()
	{
		OnPropertiesChanged();
		Property.PropertiesChanged += OnPropertiesChanged;
		Property.Clipboard.ContentChanged += OnClipboardContentChanged;

		GSessionManager.ActionRejected += OnActionRejected;
	}

	private static PropertyTool Property => GToolManager.Property;

	[ObservableProperty] public partial string FilterQuery { get; set; } = string.Empty;
	[ObservableProperty] public partial string? Notice { get; private set; }

	[ObservableProperty] public partial bool IsCopyEnabled { get; private set; }
	[ObservableProperty] public partial bool IsPasteEnabled { get; private set; }

	public ObservableCollection<PropertyItem> VisibleItems { get; } = [];

	// Where the Asset Selector opens, as their tools are rarely enabled together
	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(0, 21, 7, 11))
	{
		Title = "Prop. Editor",
		Description = "Lets you view and edit the selected assets' properties.",
		MinSize = new Size(320, 192)
	};

	protected override void OnDispose()
	{
		Property.PropertiesChanged -= OnPropertiesChanged;
		Property.Clipboard.ContentChanged -= OnClipboardContentChanged;

		GSessionManager.ActionRejected -= OnActionRejected;
	}

	// ReSharper disable once UnusedParameterInPartialMethod
	partial void OnFilterQueryChanged(string value) => Filter();

	[RelayCommand]
	private static void Copy() => Property.Copy();

	[RelayCommand]
	private static void Paste() => Property.Paste();

	private static PropertyItem CreateItem(PropertyKey key) =>
		key.Type switch
		{
			CoreVariantType.Boolean => new TogglePropertyItem(key),
			CoreVariantType.Int64 => new IntegerPropertyItem(key),
			CoreVariantType.Double => new NumberPropertyItem(key),
			_ => new TextPropertyItem(key)
		};

	private void OnPropertiesChanged()
	{
		var keys = new HashSet<PropertyKey>();

		foreach (var property in Property.Properties)
		{
			if (!_itemsByKey.TryGetValue(property.Key, out var item))
				_itemsByKey.Add(property.Key, item = CreateItem(property.Key));

			item.Sync(property);
			keys.Add(property.Key);
		}

		foreach (var (key, _) in _itemsByKey)
			if (!keys.Contains(key))
				_itemsByKey.Remove(key);

		IsCopyEnabled = Property.Properties.Any(static property => !property.IsMixed);
		UpdatePaste();

		Filter();
	}

	private void Filter()
	{
		var query = FilterQuery.Trim();

		PropertyItem[] items =
		[
			.. _itemsByKey.Values
				.Where(item => item.Matches(query))
				.OrderBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
				.ThenBy(static item => item.Key.Type)
		];

		Notice = items.Length > 0 ? null : GetNotice();

		// Kept as is when unchanged, so an edit's echo doesn't recreate the editor in use
		if (items.SequenceEqual(VisibleItems))
			return;

		VisibleItems.Clear();

		foreach (var item in items)
			VisibleItems.Add(item);
	}

	private string GetNotice()
	{
		if (GToolManager.Selector.InstanceIds.Count is 0)
			return "Select assets to edit their properties.";

		return _itemsByKey.Count is 0
			? "The selected assets have no editable properties."
			: "No properties match the filter.";
	}

	private void UpdatePaste() =>
		IsPasteEnabled = Property.Clipboard.Contents.Count > 0 && GToolManager.Selector.InstanceIds.Count > 0;

	private void OnClipboardContentChanged(PropertyKey _, CoreVariant __) => UpdatePaste();

	// The editors already show the rejected values, so they're set back
	private void OnActionRejected(string actionId, string reason)
	{
		if (actionId is nameof(SetPropertiesAction))
			OnPropertiesChanged();
	}
}

public abstract partial class PropertyItem(PropertyKey key) : ObservableObject
{
	private bool _isSyncing;

	public PropertyKey Key { get; } = key;
	public string DisplayName { get; } = key.Name.ToDisplayName();
	public string Description => $"{Key.Name} ({TypeName})";

	public string Placeholder => IsMixed ? "<Mixed>" : $"<{TypeName}>";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Placeholder))]
	public partial bool IsMixed { get; private set; }

	protected abstract string TypeName { get; }

	// Mixed values show blank (null), as nothing is shared to show
	internal void Sync(EditableProperty property)
	{
		_isSyncing = true;

		IsMixed = property.IsMixed;
		Load(property.IsMixed ? CoreVariant.Null : property.Value);

		_isSyncing = false;
	}

	internal bool Matches(string query) =>
		DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
		Key.Name.Contains(query, StringComparison.OrdinalIgnoreCase);

	protected abstract void Load(CoreVariant value);

	protected void Commit(CoreVariant value)
	{
		if (!_isSyncing)
			GToolManager.Property.SetProperty(Key.Name, value);
	}
}

public sealed partial class TogglePropertyItem(PropertyKey key) : PropertyItem(key)
{
	[ObservableProperty] public partial bool? Value { get; set; }

	protected override string TypeName => "Toggle";

	protected override void Load(CoreVariant value) => Value = value.IsNull ? null : (bool)value;

	partial void OnValueChanged(bool? value)
	{
		if (value is { } changed)
			Commit(changed);
	}
}

public sealed partial class IntegerPropertyItem(PropertyKey key) : PropertyItem(key)
{
	[ObservableProperty] public partial decimal? Value { get; set; }

	protected override string TypeName => "Integer";

	protected override void Load(CoreVariant value) => Value = value.IsNull ? null : (long)value;

	partial void OnValueChanged(decimal? value)
	{
		if (value is { } changed)
			Commit(decimal.ToInt64(decimal.Round(changed)));
	}
}

public sealed partial class NumberPropertyItem(PropertyKey key) : PropertyItem(key)
{
	// Decimals span less than doubles, and assets clamp what they read anyway
	private const double Limit = 1e28;

	[ObservableProperty] public partial decimal? Value { get; set; }

	protected override string TypeName => "Number";

	protected override void Load(CoreVariant value) =>
		Value = value.IsNull ? null : (decimal)Math.Clamp((double)value, -Limit, Limit);

	partial void OnValueChanged(decimal? value)
	{
		if (value is { } changed)
			Commit((double)changed);
	}
}

public sealed partial class TextPropertyItem(PropertyKey key) : PropertyItem(key)
{
	[ObservableProperty] public partial string Value { get; set; } = string.Empty;

	protected override string TypeName => "Text";

	protected override void Load(CoreVariant value) => Value = value.IsNull ? string.Empty : (string)value;

	partial void OnValueChanged(string value) => Commit(value);
}
