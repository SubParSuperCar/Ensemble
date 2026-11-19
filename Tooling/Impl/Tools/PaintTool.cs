using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.Common.Input;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets.Components;
using EnsembleRoot.Scripts.Assets.Materials;
using Godot;
using Serilog;

namespace EnsembleRoot.Tooling.Tools;

/// <summary>
///     Paints the local plot's paintable instances with the brush: its color and material, each only if applied. A
///     press paints the hovered instance and a drag every one it sweeps, as one edit on release; a marquee paints its
///     box. The eyedropper, toggled by tapping the marquee key alone, takes the brush from an instance instead: the
///     next press while it's on, or a marquee click, after which it turns off so the next press paints.
/// </summary>
public partial class PaintTool : ToolBase
{
	public static readonly StringName ToggleAction = "tool_paint_toggle";
	public static readonly Color Theme = new("#E050E0");

	private static readonly StringName CancelAction = "ui_cancel";

	private readonly SelectionHighlights _highlights = new()
	{
		Name = "Highlights", Filter = static handle => GAssetManager.IsPaintable(handle.AssetId)
	};

	private readonly MultiSelector _stroke = new();

	private bool _isMarqueeKeyTapped;
	private bool _isStroking;

	public override Color ThemeColor => Theme;
	public override bool UsesMarquee => true;

	public Color Color
	{
		get;
		set => SetBrush(ref field, value);
	} = Colors.White;

	public int MaterialId
	{
		get;
		set => SetBrush(ref field, value);
	} = None;

	public bool IsColorApplied
	{
		get;
		set => SetBrush(ref field, value);
	} = true;

	public bool IsMaterialApplied
	{
		get;
		set => SetBrush(ref field, value);
	} = true;

	public bool IsEyedropping
	{
		get;
		set => SetBrush(ref field, value);
	}

	/// <summary>Raised when the brush or the eyedropper changes.</summary>
	public event Action? BrushChanged;

	/// <summary>Raised when the brush is submitted onto instances.</summary>
	public event Action? Painted;

	public override void _EnterTree() => _stroke.Initialize();
	public override void _Ready() => AddChild(_highlights);
	public override void _ExitTree() => _stroke.Dispose();

	// Releases land here even over the UI, so a stroke always ends, and cancels before Esc's other uses
	public override void _Input(InputEvent @event)
	{
		UpdateEyedropperTap(@event);

		if (_isStroking && @event.IsActionReleased(ToolCommon.TriggerAction))
			FinishStroke();
		else if (@event.IsActionPressed(CancelAction) && (_isStroking || IsEyedropping))
		{
			GetViewport().SetInputAsHandled();

			if (_isStroking)
				CancelStroke();
			else
				IsEyedropping = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsEnabled || InputSink.IsSunk)
			return;

		if (!ToolCommon.IsTriggerPressed(@event))
			return;

		if (IsEyedropping)
		{
			SampleHovered();
			return;
		}

		_isStroking = true;
		Sweep();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isStroking)
			Sweep();
	}

	/// <summary>Paints every paintable instance of the local plot.</summary>
	/// <returns>Whether there was a plot to paint: one that's claimed and not spawned.</returns>
	public bool PaintAll()
	{
		if (IsLocalPlotSpawned is not false || LocalPlot is not { } plot)
			return false;

		Paint([.. plot.Instances.Source.All.Select(static instance => instance.Id)]);
		return true;
	}

	internal override void OnMarqueeFinished(IReadOnlyList<int> instanceIds, bool isToggling)
	{
		if (!IsEyedropping)
			Paint(instanceIds);
	}

	internal override void OnMarqueeClicked() => SampleHovered();

	protected override void OnEnable()
	{
		UpdateTint();
		_highlights.Attach(_stroke);
	}

	protected override void OnDisable()
	{
		CancelStroke();
		_highlights.Detach();

		IsEyedropping = false;
	}

	private void SetBrush<T>(ref T field, T value)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
			return;

		field = value;
		UpdateTint();

		BrushChanged?.Invoke();
	}

	// The highlights preview the paint's color, opaque, as their fill has its own opacity
	private void UpdateTint() => _highlights.Tint = IsColorApplied && !IsEyedropping ? Color with { A = 1 } : Theme;

	// Any other key or button pressed while it's held makes it a modifier, so a marquee or shortcut toggles nothing
	private void UpdateEyedropperTap(InputEvent @event)
	{
		if (@event.IsActionPressed(ToolCommon.SelectionMarqueeAction))
			_isMarqueeKeyTapped = IsEnabled && !_isStroking && !InputSink.IsSunk;
		else if (@event.IsActionReleased(ToolCommon.SelectionMarqueeAction))
		{
			if (_isMarqueeKeyTapped && IsEnabled)
				IsEyedropping = !IsEyedropping;

			_isMarqueeKeyTapped = false;
		}
		else if (@event is InputEventKey { Pressed: true, Echo: false } or InputEventMouseButton { Pressed: true })
			_isMarqueeKeyTapped = false;
	}

	private void Sweep()
	{
		if (_highlights.HoveredId is { } instanceId)
			_stroke.Add([instanceId]);
	}

	private void FinishStroke()
	{
		_isStroking = false;
		Paint([.. _stroke.InstanceIds]);

		_stroke.Clear();
	}

	private void CancelStroke()
	{
		_isStroking = false;
		_stroke.Clear();
	}

	private void Paint(IReadOnlyCollection<int> instanceIds)
	{
		if (instanceIds.Count is 0 || LocalPlot is not { } plot)
			return;

		var edits = new List<(GdInstance, IReadOnlyDictionary<string, CoreVariant>)>();

		foreach (var instance in instanceIds.Select(plot.Instances.GetInstance).OfType<GdInstance>())
			if (GetChanges(instance) is { Count: > 0 } changes)
				edits.Add((instance, changes));

		ToolCommon.PlaySound(edits.Count > 0 ? ToolCommon.AffirmSound : ToolCommon.DissentSound);

		if (edits.Count is 0)
			return;

		UndoRedo.SetProperties(edits);
		Log.Verbose("Submitted paint for {Count} instance(s)", edits.Count);

		Painted?.Invoke();
	}

	private Dictionary<string, CoreVariant> GetChanges(GdInstance instance)
	{
		var changes = new Dictionary<string, CoreVariant>(IProperties.KeyComparer);

		if (!GAssetManager.IsPaintable(instance.Asset.Id))
			return changes;

		var properties = instance.Properties.Source.All;
		var hasAlpha = GAssetManager.HasPaintAlpha(instance.Asset.Id);
		var hex = Paintable.WriteColor(Color, hasAlpha);

		if (
			IsColorApplied && properties.TryGetValue(Paintable.ColorKey, out var color) &&
			color.Value is string current && !string.Equals(Rewrite(current, hasAlpha), hex, StringComparison.Ordinal))
			changes[Paintable.ColorKey] = hex;

		if (
			IsMaterialApplied && properties.TryGetValue(MaterialBank.PropertyKey, out var material) &&
			material.Value is long && material != MaterialId)
			changes[MaterialBank.PropertyKey] = MaterialId;

		return changes;
	}

	// So the same color written another way isn't an edit
	private static string Rewrite(string hex, bool hasAlpha) =>
		Paintable.WriteColor(Paintable.ReadColor(hex, hasAlpha), hasAlpha);

	private void SampleHovered()
	{
		if (Sample(_highlights.HoveredId))
			IsEyedropping = false;
	}

	private bool Sample(int? instanceId)
	{
		if (
			instanceId is not { } id || LocalPlot?.Instances.GetInstance(id) is not { } instance ||
			!GAssetManager.IsPaintable(instance.Asset.Id))
		{
			ToolCommon.PlaySound(ToolCommon.DissentSound);
			return false;
		}

		var properties = instance.Properties.Source.All;

		if (IsColorApplied && properties.TryGetValue(Paintable.ColorKey, out var color) && color.Value is string hex)
			Color = Paintable.ReadColor(hex, GAssetManager.HasPaintAlpha(instance.Asset.Id));

		if (
			IsMaterialApplied && properties.TryGetValue(MaterialBank.PropertyKey, out var material) &&
			material.Value is long materialId)
			MaterialId = (int)materialId;

		ToolCommon.PlaySound(ToolCommon.AffirmSound);
		Log.Verbose("Sampled the brush from instance {InstanceId}", id);

		return true;
	}
}
