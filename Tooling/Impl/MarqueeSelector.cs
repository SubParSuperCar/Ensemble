using EnsembleRoot.Common.Input;
using EnsembleRoot.Tooling.Tools;
using Godot;
using Serilog;

namespace EnsembleRoot.Tooling;

/// <summary>
///     Box-selects the local plot's instances whose centers fall within a dragged viewport area (Baja Builders parity).
///     Dragging with the marquee key held sets the selection, or toggles it with the selection toggle key held on
///     release. Releasing the marquee key or pressing cancel aborts the drag.
/// </summary>
public partial class MarqueeSelector : Node
{
	private static readonly StringName CancelAction = "ui_cancel";

	private ToolManager _manager = null!;
	private Vector2? _origin;

	/// <summary>The dragged area in viewport pixels, or null unless a drag spans one.</summary>
	public Rect2? Area { get; private set; }

	public Color Color { get; private set; } = Colors.White;

	public bool CanSelect => _manager.EnabledTools.Any(static tool => tool.UsesSelection);

	public event Action<Rect2?>? AreaChanged;

	public override void _Ready() => SetProcess(false);

	public override void _Input(InputEvent @event)
	{
		if (_origin is null)
			return;

		if (@event.IsActionPressed(CancelAction))
		{
			GetViewport().SetInputAsHandled();
			Stop();
		}
		else if (@event.IsActionReleased(ToolCommon.TriggerAction))
		{
			Finish();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (
			_origin is not null || InputSink.IsSunk || !CanSelect ||
			!Input.IsActionPressed(ToolCommon.SelectionMarqueeAction) ||
			!@event.IsActionPressed(ToolCommon.TriggerAction))
			return;

		GetViewport().SetInputAsHandled();

		_origin = Pointer.GetPosition(GetViewport());
		Color = _manager.EnabledTools[^1].ThemeColor;

		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		if (_origin is not { } origin)
			return;

		if (!CanSelect || !Input.IsActionPressed(ToolCommon.SelectionMarqueeAction))
		{
			Stop();
			return;
		}

		var area = new Rect2(origin, Pointer.GetPosition(GetViewport()) - origin).Abs();
		SetArea(area.HasArea() ? area : null);
	}

	internal void Initialize(ToolManager manager) => _manager = manager;

	private void Finish()
	{
		var area = Area;
		Stop();

		if (area is not { } rect)
			return;

		var instanceIds = ToolCommon.FindInstancesInArea(GetViewport(), rect);

		if (Input.IsActionPressed(ToolCommon.SelectionToggleAction))
			_manager.Selector.Toggle(instanceIds);
		else
			_manager.Selector.Set(instanceIds);

		Log.Verbose("Marquee-selected {Count} instance(s)", instanceIds.Count);
	}

	private void Stop()
	{
		_origin = null;
		SetProcess(false);

		SetArea(null);
	}

	private void SetArea(Rect2? area)
	{
		if (Area == area)
			return;

		Area = area;
		AreaChanged?.Invoke(area);
	}
}
