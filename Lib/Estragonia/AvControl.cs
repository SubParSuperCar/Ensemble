using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Estragonia.Input;
using Godot;
using Godot.NativeInterop;
using AvControl = Avalonia.Controls.Control;
using GdControl = Godot.Control;
using GdInput = Godot.Input;
using GdKey = Godot.Key;

namespace Estragonia;

/// <summary>Renders an Avalonia control and forwards input to it.</summary>
public class AvaloniaControl : GdControl
{
	private GodotTopLevel? _topLevel;

	/// <summary>Gets or sets the underlying Avalonia control that will be rendered.</summary>
	public AvControl? Control
	{
		get;
		set
		{
			if (ReferenceEquals(field, value))
				return;

			field = value;

			_topLevel?.Content = field;
		}
	}

	/// <summary>Gets or sets the render scaling for the Avalonia control. Defaults to 1.0.</summary>
	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Doesn't affect correctness")]
	// ReSharper disable once MemberCanBePrivate.Global
	public double RenderScaling
	{
		get;
		// ReSharper disable once PropertyCanBeMadeInitOnly.Global
		// ReSharper disable once UnusedMember.Global
		set
		{
			if (field == value)
				return;

			field = value;
			OnResized();
			QueueRedraw();
		}
	} = 1.0;

	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args,
		out godot_variant ret)
	{
		if (method == Node.MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default;
			return true;
		}

		if (method == Node.MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(args[0]));
			ret = default;
			return true;
		}

		if (method == CanvasItem.MethodName._Draw && args.Count == 0)
		{
			_Draw();
			ret = default;
			return true;
		}

		if (method == MethodName._GuiInput && args.Count == 1)
		{
			_GuiInput(VariantUtils.ConvertTo<InputEvent>(args[0]));
			ret = default;
			return true;
		}

		if (method != MethodName._HasPoint || args.Count != 1)
			return base.InvokeGodotClassMethod(method, args, out ret);
		ret = VariantUtils.CreateFrom(_HasPoint(VariantUtils.ConvertTo<Vector2>(args[0])));
		return true;
	}

	protected override bool HasGodotClassMethod(in godot_string_name method) =>
		method == Node.MethodName._Ready
		|| method == Node.MethodName._Process
		|| method == CanvasItem.MethodName._Draw
		|| method == MethodName._GuiInput
		|| method == MethodName._HasPoint
		|| base.HasGodotClassMethod(method);

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		// Skia outputs a premultiplied alpha image, ensure we got the correct blend mode if the user didn't specify any
		Material ??= new CanvasItemMaterial
		{
			BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha,
			LightMode = CanvasItemMaterial.LightModeEnum.Unshaded
		};

		var locator = AvaloniaLocator.Current;

		if (locator.GetService<IPlatformGraphics>() is not IGodotPlatformGraphics graphics)
		{
			GD.PrintErr(
				"No Godot platform graphics found, did you forget to register your Avalonia app with UseGodot()?");
			return;
		}

		var topLevelImpl =
			new GodotTopLevelImpl(graphics, locator.GetRequiredService<IClipboard>(), GodotPlatform.Compositor)
			{
				CursorChanged = OnAvaloniaCursorChanged
			};

		topLevelImpl.SetRenderSize(GetFrameSize(), RenderScaling);

		_topLevel = new GodotTopLevel(topLevelImpl)
		{
			Background = null,
			Content = Control,
			TransparencyLevelHint = [WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None]
		};

		_topLevel.Prepare();
		_topLevel.StartRendering();

		Resized += OnResized;
		FocusEntered += OnFocusEntered;
		FocusExited += OnFocusExited;
		MouseExited += OnMouseExited;

		if (HasFocus())
			OnFocusEntered();
	}

	public override void _Process(double delta)
	{
		GodotPlatform.TriggerRenderTick();

		// We might have cleared the texture after resize to prevent corruption on AMD GPU (see GodotSkiaGpuRenderSession),
		// force a re-render.
		if (_topLevel?.Impl.TryGetSurface()?.DrawCount <= 2)
			RenderAvalonia();
	}

	private PixelSize GetFrameSize() => PixelSize.FromSize(Size.ToAvaloniaSize(), 1.0);

	private void RenderAvalonia() => _topLevel!.Impl.OnDraw(new Rect(Size.ToAvaloniaSize()));

	private void OnAvaloniaCursorChanged(CursorShape cursor) => MouseDefaultCursorShape = cursor;

	private void OnResized()
	{
		if (_topLevel is null)
			return;

		_topLevel.Impl.SetRenderSize(GetFrameSize(), RenderScaling);
		RenderAvalonia();
	}

	private void OnFocusEntered()
	{
		if (_topLevel is null)
			return;

		_topLevel.Focus();

		if (KeyboardNavigationHandler.GetNext(_topLevel, NavigationDirection.Next) is not { } inputElement)
			return;

		NavigationMethod navigationMethod;

		if (GdInput.IsActionPressed(GodotBuiltInActions.UiFocusNext) ||
			GdInput.IsActionPressed(GodotBuiltInActions.UiFocusPrev))
			navigationMethod = NavigationMethod.Tab;
		else if (GdInput.GetMouseButtonMask() != 0)
			navigationMethod = NavigationMethod.Pointer;
		else
			navigationMethod = NavigationMethod.Unspecified;

		inputElement.Focus(navigationMethod);
	}

	private void OnFocusExited() => _topLevel?.Impl.OnLostFocus();

	public override void _Draw()
	{
		if (_topLevel is null)
			return;

		var surface = _topLevel.Impl.GetOrCreateSurface();

		DrawTexture(surface.GdTexture, Vector2.Zero);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (_topLevel is null)
			return;

		if (TryHandleInput(_topLevel.Impl, @event) || TryHandleAction(@event))
			AcceptEvent();
	}

	private bool TryHandleAction(InputEvent inputEvent)
	{
		if (!inputEvent.IsActionType())
			return false;

		if (inputEvent.IsActionPressed(GodotBuiltInActions.UiFocusNext, true, true))
			return TryMoveFocus(NavigationDirection.Next, inputEvent);

		if (inputEvent.IsActionPressed(GodotBuiltInActions.UiFocusPrev, true, true))
			return TryMoveFocus(NavigationDirection.Previous, inputEvent);

		if (inputEvent.IsActionPressed(GodotBuiltInActions.UiLeft, true, true))
			return SimulateKeyDownFromAction(inputEvent, GdKey.Left);

		if (inputEvent.IsActionPressed(GodotBuiltInActions.UiRight, true, true))
			return SimulateKeyDownFromAction(inputEvent, GdKey.Right);

		if (inputEvent.IsActionPressed(GodotBuiltInActions.UiUp, true, true))
			return SimulateKeyDownFromAction(inputEvent, GdKey.Up);

		if (inputEvent.IsActionPressed(GodotBuiltInActions.UiDown, true, true))
			return SimulateKeyDownFromAction(inputEvent, GdKey.Down);

		if (inputEvent.IsActionPressed(GodotBuiltInActions.UiAccept, true, true))
			return SimulateKeyDownFromAction(inputEvent, GdKey.Enter);

		return inputEvent.IsActionPressed(GodotBuiltInActions.UiCancel, true, true) &&
			   SimulateKeyDownFromAction(inputEvent, GdKey.Escape);
	}

	private bool SimulateKeyDownFromAction(InputEvent inputEvent, GdKey key)
	{
		// if the action already matches the key we're going to simulate, abort: it already got through TryHandleInput and wasn't handled
		if (inputEvent is InputEventKey inputEventKey && inputEventKey.Keycode == key)
			return false;

		if (_topLevel?.FocusManager?.GetFocusedElement() is not { } currentElement)
			return false;

		var args = new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key.ToAvaloniaKey(),
			KeyModifiers = inputEvent.GetKeyModifiers()
		};
		currentElement.RaiseEvent(args);
		return args.Handled;
	}

	private static bool TryHandleInput(GodotTopLevelImpl impl, InputEvent inputEvent) =>
		inputEvent switch
		{
			InputEventMouseMotion mouseMotion => impl.OnMouseMotion(mouseMotion, Time.GetTicksMsec()),
			InputEventMouseButton mouseButton => impl.OnMouseButton(mouseButton, Time.GetTicksMsec()),
			InputEventScreenTouch screenTouch => impl.OnScreenTouch(screenTouch, Time.GetTicksMsec()),
			InputEventScreenDrag screenDrag => impl.OnScreenDrag(screenDrag, Time.GetTicksMsec()),
			InputEventKey key => impl.OnKey(key, Time.GetTicksMsec()),
			InputEventJoypadButton joypadButton => impl.OnJoypadButton(joypadButton, Time.GetTicksMsec()),
			InputEventJoypadMotion joypadMotion => impl.OnJoypadMotion(joypadMotion, Time.GetTicksMsec()),
			_ => false
		};

	private bool TryMoveFocus(NavigationDirection direction, InputEvent inputEvent)
	{
		if (_topLevel?.FocusManager is not { } focusManager)
			return false;

		var currentElement = focusManager.GetFocusedElement() ?? _topLevel;

		// GodotTopLevel has a Continue tab navigation since we want to be able to focus the Godot controls
		// once we're done with the Avalonia ones. However, if there's no Godot control, we want to act as Cycle.
		var nextElement = GetNextTabElement(currentElement, direction);
		if (nextElement is null)
		{
			var nextGdControl = direction switch
			{
				NavigationDirection.Next => FindNextValidFocus(),
				NavigationDirection.Previous => FindPrevValidFocus(),
				_ => null
			};

			if ((nextGdControl is null || nextGdControl == this) && (object)currentElement != _topLevel)
				nextElement = GetNextTabElement(_topLevel, direction);
		}


		if (nextElement is null)
			return false;

		nextElement.Focus(NavigationMethod.Tab, inputEvent.GetKeyModifiers());
		return true;
	}

	private static IInputElement? GetNextTabElement(IInputElement element, NavigationDirection direction)
	{
		var previous = element;

		while (true)
		{
			// GetNext doesn't take IsEffectivelyEnabled into account, check it manually
			var next = KeyboardNavigationHandler.GetNext(previous, direction);
			if (next is null || next.IsEffectivelyEnabled)
				return next;

			// handle potential all-disabled cycle
			if (next == element)
				return null;

			previous = next;
		}
	}

	private void OnMouseExited() => _topLevel?.Impl.OnMouseExited(Time.GetTicksMsec());

	public override bool _HasPoint(Vector2 point) =>
		_topLevel?.InputHitTest(point.ToAvaloniaPoint() / _topLevel.RenderScaling, false) is not null;

	protected override void Dispose(bool disposing)
	{
		if (disposing && _topLevel is not null)
		{
			Resized -= OnResized;
			FocusEntered -= OnFocusEntered;
			FocusExited -= OnFocusExited;
			MouseExited -= OnMouseExited;

			_topLevel.Dispose();
			_topLevel = null;
		}

		base.Dispose(disposing);
	}
}
