using Godot;

namespace Estragonia.Input;

/// <summary>Contains the names of various Godot built-in actions that are recognized by Estragonia.</summary>
public static class GodotBuiltInActions
{
	/// <summary>Action name: ui_focus_next</summary>
	public static readonly StringName UiFocusNext = "ui_focus_next";

	/// <summary>Action name: ui_focus_prev</summary>
	public static readonly StringName UiFocusPrev = "ui_focus_prev";

	/// <summary>Action name: ui_left</summary>
	public static readonly StringName UiLeft = "ui_left";

	/// <summary>Action name: ui_right</summary>
	public static readonly StringName UiRight = "ui_right";

	/// <summary>Action name: ui_up</summary>
	public static readonly StringName UiUp = "ui_up";

	/// <summary>Action name: ui_down</summary>
	public static readonly StringName UiDown = "ui_down";

	/// <summary>Action name: ui_accept</summary>
	public static readonly StringName UiAccept = "ui_accept";

	/// <summary>Action name: ui_cancel</summary>
	public static readonly StringName UiCancel = "ui_cancel";
}
