using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Common.Input;
using EnsembleRoot.GdCore.Assets;
using Godot;
using Serilog;

// ReSharper disable EventNeverSubscribedTo.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace EnsembleRoot.Tooling.Tools;

/// <summary>A property as the EditTool tells them apart: by name, ignoring case as instances do, and by type.</summary>
public readonly record struct PropertyKey(string Name, CoreVariantType Type)
{
	public bool Equals(PropertyKey other) =>
		Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

	public override int GetHashCode() => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Name), Type);
}

/// <param name="IsMixed">Whether the instances differ, so <paramref name="Value" /> is the default instead.</param>
public readonly record struct EditableProperty(PropertyKey Key, CoreVariant Value, bool IsMixed);

/// <summary>
///     Edits the selected instances' properties, except the engine's own (prefixed with an underscore). The instances
///     that share a property (by name and type) show one value: theirs if they agree, the default otherwise.
/// </summary>
public partial class EditTool : ToolBase
{
	public static readonly StringName ToggleAction = "tool_edit_toggle";
	public static readonly Color Theme = new("#00C000");

	private static readonly StringName CopyAction = "tool_edit_copy";
	private static readonly StringName PasteAction = "tool_edit_paste";

	private readonly SelectionHighlights _highlights = new() { Name = "Highlights", Tint = Theme };
	private readonly Dictionary<int, IProperties> _watchedProperties = [];

	private bool _isStale;

	public override Color ThemeColor => Theme;
	public override bool UsesSelection => true;

	public Clipboard<PropertyKey, CoreVariant> Clipboard { get; } = new();

	public IReadOnlyList<EditableProperty> Properties { get; private set; } = [];

	public event Action? PropertiesChanged;

	public override void _Ready() => AddChild(_highlights);

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsEnabled || InputSink.IsSunk)
			return;

		if (@event.IsActionPressed(CopyAction))
			Copy();
		else if (@event.IsActionPressed(PasteAction))
			Paste();
		else if (ToolCommon.IsTriggerPressed(@event))
			Select(_highlights.HoveredId, Input.IsActionPressed(ToolCommon.SelectionToggleAction));
	}

	// Once per frame, however many properties a bulk edit changes
	public override void _Process(double delta)
	{
		if (!_isStale)
			return;

		_isStale = false;
		Properties = Merge();

		PropertiesChanged?.Invoke();
	}

	/// <summary>Sets a property on each selected instance that has it under the same name and type.</summary>
	public void SetProperty(string name, CoreVariant value) =>
		Apply(new Dictionary<PropertyKey, CoreVariant> { [new PropertyKey(name, value.Type)] = value });

	protected override void OnEnable()
	{
		_highlights.Attach(Selector);

		OnSelectionAdded([.. Selector.InstanceIds]);
		Selector.Added += OnSelectionAdded;
		Selector.Removed += OnSelectionRemoved;
	}

	protected override void OnDisable()
	{
		_highlights.Detach();

		Selector.Added -= OnSelectionAdded;
		Selector.Removed -= OnSelectionRemoved;
		OnSelectionRemoved([.. _watchedProperties.Keys]);

		_isStale = false;
		Properties = [];

		PropertiesChanged?.Invoke();
	}

	private static bool IsEditable(string name, CoreVariant value) => !name.StartsWith('_') && !value.IsNull;

	// A value the instances disagree on turns null
	private static CoreVariant Agree(CoreVariant left, CoreVariant right) => left == right ? left : CoreVariant.Null;

	// Assets that disagree on the default too fall back to the type's
	private static EditableProperty ToProperty(PropertyKey key, CoreVariant value, CoreVariant initial) =>
		value.IsNull
			? new EditableProperty(key, initial.IsNull ? GetZero(key.Type) : initial, true)
			: new EditableProperty(key, value, false);

	private static CoreVariant GetZero(CoreVariantType type) =>
		type switch
		{
			CoreVariantType.Boolean => false,
			CoreVariantType.Int64 => 0L,
			CoreVariantType.Double => 0d,
			CoreVariantType.String => string.Empty,
			_ => CoreVariant.Null
		};

	private IEnumerable<GdInstance> GetSelected() =>
		LocalPlot is { } plot ? Selector.InstanceIds.Select(plot.Instances.GetInstance).OfType<GdInstance>() : [];

	private List<EditableProperty> Merge()
	{
		var merged = new OrderedDictionary<PropertyKey, (CoreVariant Value, CoreVariant Default)>();

		foreach (var instance in GetSelected())
		{
			var defaults = instance.Asset.Source.Properties;

			foreach (var (name, value) in instance.Properties.Source.All)
			{
				if (!IsEditable(name, value))
					continue;

				var key = new PropertyKey(name, value.Type);
				var initial = defaults.GetValueOrDefault(name);

				merged[key] = merged.TryGetValue(key, out var entry)
					? (Agree(entry.Value, value), Agree(entry.Default, initial))
					: (value, initial);
			}
		}

		return [.. merged.Select(static pair => ToProperty(pair.Key, pair.Value.Value, pair.Value.Default))];
	}

	// Each instance takes the values of the properties it has under the same names and types
	private bool Apply(IReadOnlyDictionary<PropertyKey, CoreVariant> values)
	{
		List<(GdInstance, IReadOnlyDictionary<string, CoreVariant>)> edits = [];

		foreach (var instance in GetSelected())
		{
			var changes = new Dictionary<string, CoreVariant>(StringComparer.OrdinalIgnoreCase);

			foreach (var (name, current) in instance.Properties.Source.All)
				if (
					IsEditable(name, current) &&
					values.TryGetValue(new PropertyKey(name, current.Type), out var value) &&
					value != current)
					changes[name] = value;

			if (changes.Count > 0)
				edits.Add((instance, changes));
		}

		if (edits.Count is 0)
			return false;

		UndoRedo.SetProperties(edits);
		Log.Verbose("Submitted property edits for {Count} instance(s)", edits.Count);

		return true;
	}

	// Mixed values are left out, so pasting never spreads a default the source instances didn't have
	private void Copy()
	{
		var properties = Merge()
			.Where(static property => !property.IsMixed)
			.ToDictionary(static property => property.Key, static property => property.Value);

		if (properties.Count > 0)
			Clipboard.SetAll(properties);

		ToolCommon.PlaySound(properties.Count > 0 ? ToolCommon.AffirmSound : ToolCommon.DissentSound);
		Log.Verbose("Copied {Count} property value(s)", properties.Count);
	}

	private void Paste() =>
		ToolCommon.PlaySound(Apply(Clipboard.Contents) ? ToolCommon.AffirmSound : ToolCommon.DissentSound);

	// A press selects only the hovered instance, or toggles it, and a press on nothing clears the selection
	private void Select(int? hoveredId, bool isToggling)
	{
		switch (hoveredId)
		{
			case { } id when isToggling:
				Selector.Toggle([id]);
				break;

			case { } id:
				Selector.Set([id]);
				break;

			case null when !isToggling:
				Selector.Clear();
				break;
		}
	}

	private void OnSelectionAdded(IReadOnlyList<int> instanceIds)
	{
		foreach (var id in instanceIds)
			if (
				LocalPlot?.Instances.Source.TryGet(id, out var instance) is true &&
				_watchedProperties.TryAdd(id, instance.Properties))
				instance.Properties.Changed += OnPropertyChanged;

		_isStale = true;
	}

	private void OnSelectionRemoved(IReadOnlyList<int> instanceIds)
	{
		foreach (var id in instanceIds)
			if (_watchedProperties.Remove(id, out var properties))
				properties.Changed -= OnPropertyChanged;

		_isStale = true;
	}

	private void OnPropertyChanged(string _, CoreVariant __) => _isStale = true;
}
