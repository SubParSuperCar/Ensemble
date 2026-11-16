using EnsembleRoot.Common.Input;
using EnsembleRoot.Tooling.Tools;
using Godot;

namespace EnsembleRoot.Tooling;

/// <summary>
///     Finds the local plot's instances whose centers fall within a viewport area dragged with the marquee key held
///     (Baja Builders parity), for the last enabled tool that uses it: tools that select set the selection, or toggle
///     it with the selection toggle key held on release. Releasing the marquee key or pressing cancel aborts the drag.
/// </summary>
public partial class MarqueeSelector : Node
{
	// How far a press may wander, in pixels, and still count as a click rather than a drag
	private const float ClickSlop = 4f;

	private static readonly StringName CancelAction = "ui_cancel";

	private ToolManager _manager = null!;
	private Vector2? _origin;

	/// <summary>The dragged area in viewport pixels, or null unless a drag spans one.</summary>
	public Rect2? Area { get; private set; }

	public Color Color { get; private set; } = Colors.White;

	public bool CanStart => _manager.EnabledTools.Any(static tool => tool.UsesMarquee);

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
			Finish();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (
			_origin is not null || InputSink.IsSunk || !@event.IsActionPressed(ToolCommon.TriggerAction) ||
			!Input.IsActionPressed(ToolCommon.SelectionMarqueeAction) || !CanStart)
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

		if (!CanStart || !Input.IsActionPressed(ToolCommon.SelectionMarqueeAction))
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

		if (_manager.EnabledTools.LastOrDefault(static tool => tool.UsesMarquee) is not { } tool)
			return;

		if (area is { Size: var size } rect && (size.X >= ClickSlop || size.Y >= ClickSlop))
			tool.OnMarqueeFinished(
				ToolCommon.FindInstancesInArea(GetViewport(), rect),
				Input.IsActionPressed(ToolCommon.SelectionToggleAction));
		else
			tool.OnMarqueeClicked();
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
