using System.Collections.ObjectModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Common.Input;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Tooling.Tools;
using EnsembleRoot.Ui.Abstractions;
using EnsembleRoot.Ui.Extensions;
using EnsembleRoot.Ui.Services;
using Godot;

namespace EnsembleRoot.Ui.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed partial class PropertyEditorViewModel : ViewModelBase, IWidget
{
	private readonly DispatcherService _dispatcher;
	private readonly Dictionary<PropertyKey, PropertyItem> _itemsByKey = [];

	private Key? _heldKey;
	private KeyPropertyItem? _listeningItem;

	public PropertyEditorViewModel(DispatcherService dispatcher)
	{
		_dispatcher = dispatcher;

		dispatcher.Input += OnInput;

		OnPropertiesChanged();
		Property.PropertiesChanged += OnPropertiesChanged;
		Property.Clipboard.ContentChanged += OnClipboardContentChanged;
		Property.Copied += OnCopied;
		Property.Pasted += OnPasted;

		GSessionManager.ActionRejected += OnActionRejected;
	}

	private static PropertyTool Property => GToolManager.Property;

	[ObservableProperty] public partial string FilterQuery { get; set; } = string.Empty;
	[ObservableProperty] public partial string? Notice { get; private set; }

	[ObservableProperty] public partial bool IsCopyEnabled { get; private set; }
	[ObservableProperty] public partial bool IsPasteEnabled { get; private set; }

	[ObservableProperty] public partial bool IsInternalShown { get; set; } = Property.IsInternalShown;

	public ObservableCollection<PropertyItem> VisibleItems { get; } = [];

	// Where the Asset Selector opens, as their tools are rarely enabled together
	public static WidgetDescriptor Descriptor { get; } = new(WidgetDescriptor.Cells(0, 21, 7, 11))
	{
		Title = "Prop. Editor",
		Description = "Lets you view and edit the selected assets' properties.",
		MinSize = new Size(320, 192)
	};

	public event Action? Copied;

	public event Action? Pasted;

	protected override void OnDispose()
	{
		_heldKey = null;
		StopListening();
		_dispatcher.Input -= OnInput;

		Property.PropertiesChanged -= OnPropertiesChanged;
		Property.Clipboard.ContentChanged -= OnClipboardContentChanged;
		Property.Copied -= OnCopied;
		Property.Pasted -= OnPasted;

		GSessionManager.ActionRejected -= OnActionRejected;
	}

	// ReSharper disable once UnusedParameterInPartialMethod
	partial void OnFilterQueryChanged(string value) => Filter();

	partial void OnIsInternalShownChanged(bool value) => Property.IsInternalShown = value;

	[RelayCommand]
	private static void Copy() => Property.Copy();

	[RelayCommand]
	private static void Paste() => Property.Paste();

	private PropertyItem CreateItem(PropertyKey key) =>
		key.Type switch
		{
			CoreVariantType.Boolean => new TogglePropertyItem(key),
			CoreVariantType.Int64 when InputState.IsBinding(key.Name) => new KeyPropertyItem(key, ToggleListening),
			CoreVariantType.Int64 => new IntegerPropertyItem(key),
			CoreVariantType.Double => new DecimalPropertyItem(key),
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

		foreach (var (key, item) in _itemsByKey)
			if (!keys.Contains(key))
			{
				_itemsByKey.Remove(key);

				if (item == _listeningItem)
					StopListening();
			}

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

	private void ToggleListening(KeyPropertyItem item)
	{
		var isStarting = item != _listeningItem;

		StopListening();

		if (!isStarting)
			return;

		_listeningItem = item;
		item.IsListening = true;
		InputSink.Sink.Acquire(this);
	}

	private void StopListening()
	{
		_listeningItem?.IsListening = false;
		_listeningItem = null;

		ReleaseSink();
	}

	// The sink outlasts the key press until the key comes back up, so it doesn't also reach whatever the game binds it to
	private void ReleaseSink()
	{
		if (_listeningItem is null && _heldKey is null)
			InputSink.Sink.Release(this);
	}

	private void OnInput(InputEvent @event)
	{
		// Polled, as a key released while the window is unfocused sends no event, which would leave the sink held
		if (_heldKey is { } held && !Input.IsKeyPressed(held))
		{
			_heldKey = null;
			ReleaseSink();
		}

		if (
			@event is not InputEventKey { Echo: false, Pressed: true } key || _listeningItem is not { } item ||
			(key.Keycode is not Key.Escape && !InputState.IsBindable(key.Keycode)))
			return;

		_heldKey = key.Keycode;

		if (key.Keycode is not Key.Escape)
			item.Bind(key.Keycode);

		StopListening();
	}

	private void UpdatePaste() =>
		IsPasteEnabled = Property.Clipboard.Contents.Count > 0 && GToolManager.Selector.InstanceIds.Count > 0;

	private void OnClipboardContentChanged(PropertyKey _, CoreVariant __) => UpdatePaste();

	private void OnCopied() => Copied?.Invoke();
	private void OnPasted() => Pasted?.Invoke();

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

	[ObservableProperty] public partial bool CanReset { get; private set; }

	protected abstract string TypeName { get; }

	// Mixed values show blank (null), as nothing is shared to show
	internal void Sync(EditableProperty property)
	{
		_isSyncing = true;

		IsMixed = property.IsMixed;
		CanReset = property.CanReset;
		Load(property.IsMixed ? CoreVariant.Null : property.Value);

		_isSyncing = false;
	}

	[RelayCommand]
	private void Reset() => GToolManager.Property.ResetProperty(Key);

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
	[ObservableProperty] public partial bool Value { get; set; }

	protected override string TypeName => "Toggle";

	// Mixed shows as off, with the row's warning, so one click sets every selected asset on
	protected override void Load(CoreVariant value) => Value = !value.IsNull && (bool)value;

	partial void OnValueChanged(bool value) => Commit(value);
}

public sealed partial class IntegerPropertyItem(PropertyKey key) : PropertyItem(key)
{
	[ObservableProperty] public partial decimal? Value { get; set; }

	protected override string TypeName => "Integer";

	protected override void Load(CoreVariant value) => Value = value.IsNull ? null : (long)value;

	partial void OnValueChanged(decimal? value)
	{
		if (value is { } changed)
			Commit(decimal.ToInt64(decimal.Round(changed, MidpointRounding.ToEven)));
	}
}

public sealed partial class KeyPropertyItem(PropertyKey key, Action<KeyPropertyItem> toggleListening)
	: PropertyItem(key)
{
	private Key? _value;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Text))]
	public partial bool IsListening { get; set; }

	public string Text =>
		IsListening ? "Press a Key\u2026" :
		_value is not { } value ? "<Mixed>" :
		InputState.IsBindable(value) ? OS.GetKeycodeString(value) : "<None>";

	protected override string TypeName => "Key";

	internal void Bind(Key key) => Commit((long)key);

	protected override void Load(CoreVariant value)
	{
		_value = value.IsNull ? null : (Key)(long)value;
		OnPropertyChanged(nameof(Text));
	}

	[RelayCommand]
	private void ToggleListening() => toggleListening(this);
}

public sealed partial class DecimalPropertyItem(PropertyKey key) : PropertyItem(key)
{
	// Decimals span less than doubles, and assets clamp what they read anyway
	private const double Limit = 1e28;

	[ObservableProperty] public partial decimal? Value { get; set; }

	protected override string TypeName => "Decimal";

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
