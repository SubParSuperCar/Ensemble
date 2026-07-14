using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Estragonia.Input;
using Godot;
using AvCompositor = Avalonia.Rendering.Composition.Compositor;
using AvKey = Avalonia.Input.Key;
using GdCursorShape = Godot.Control.CursorShape;
using GdMouseButton = Godot.MouseButton;

namespace Estragonia;

/// <summary>Implementation of Avalonia <see cref="ITopLevelImpl" /> that renders to a Godot texture.</summary>
internal sealed class GodotTopLevelImpl : ITopLevelImpl
{
	private readonly IClipboard _clipboard;

	private readonly IGodotPlatformGraphics _platformGraphics;
	private readonly TouchDevice _touchDevice = new();
	private GdCursorShape _cursorShape;
	private IInputRoot? _inputRoot;
	private bool _isDisposed;
	private int _lastMouseDeviceId = GodotDevices.EmulatedDeviceId;
	private PixelSize _renderSize;

	private IGodotSkiaSurface? _surface;

	public GodotTopLevelImpl(IGodotPlatformGraphics platformGraphics, IClipboard clipboard, AvCompositor compositor)
	{
		_platformGraphics = platformGraphics;
		_clipboard = clipboard;
		Compositor = compositor;

		platformGraphics.AddRef();
	}

	public Action<GdCursorShape>? CursorChanged { get; init; }

	public double RenderScaling { get; private set; } = 1.0;

	double ITopLevelImpl.DesktopScaling => 1.0;

	IPlatformHandle? ITopLevelImpl.Handle => null;

	public AvCompositor Compositor { get; }

	public Size ClientSize { get; private set; }

	public WindowTransparencyLevel TransparencyLevel
	{
		get;
		private set
		{
			if (field.Equals(value))
				return;

			field = value;
			TransparencyLevelChanged?.Invoke(value);
		}
	} = WindowTransparencyLevel.Transparent;

	public Action<Rect>? Paint { get; set; }

	public Action<Size, WindowResizeReason>? Resized { get; set; }

	public Action? Closed { get; set; }

	public Action<RawInputEventArgs>? Input { get; set; }

	public Action? LostFocus { get; set; }

	public Action<double>? ScalingChanged { get; set; }

	public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

	IEnumerable<object> ITopLevelImpl.Surfaces => GetOrCreateSurfaces();

	AcrylicPlatformCompensationLevels ITopLevelImpl.AcrylicCompensationLevels => new(1.0, 1.0, 1.0);

	void ITopLevelImpl.SetInputRoot(IInputRoot inputRoot) => _inputRoot = inputRoot;

	Point ITopLevelImpl.PointToClient(PixelPoint point) => point.ToPoint(RenderScaling);

	PixelPoint ITopLevelImpl.PointToScreen(Point point) => PixelPoint.FromPoint(point, RenderScaling);

	void ITopLevelImpl.SetCursor(ICursorImpl? cursor)
	{
		var cursorShape = (cursor as GodotStandardCursorImpl)?.CursorShape ?? GdCursorShape.Arrow;
		if (_cursorShape == cursorShape)
			return;

		_cursorShape = cursorShape;
		CursorChanged?.Invoke(cursorShape);
	}

	IPopupImpl? ITopLevelImpl.CreatePopup() => null;

	void ITopLevelImpl.SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
	{
		foreach (var transparencyLevel in transparencyLevels)
			if (transparencyLevel == WindowTransparencyLevel.Transparent ||
				transparencyLevel == WindowTransparencyLevel.None)
			{
				TransparencyLevel = transparencyLevel;
				return;
			}
	}

	void ITopLevelImpl.SetFrameThemeVariant(PlatformThemeVariant themeVariant)
	{
	}

	object? IOptionalFeatureProvider.TryGetFeature(Type featureType) =>
		featureType == typeof(IClipboard) ? _clipboard : null;

	public void Dispose()
	{
		if (_isDisposed)
			return;

		_isDisposed = true;

		if (_surface is not null)
		{
			_surface.Dispose();
			_surface = null;
		}

		Closed?.Invoke();

		_platformGraphics.Release();
	}

	private IGodotSkiaSurface CreateSurface() =>
		_isDisposed
			? throw new ObjectDisposedException(nameof(GodotTopLevelImpl))
			: _platformGraphics.GetSharedContext().CreateSurface(_renderSize, RenderScaling);

	public IGodotSkiaSurface? TryGetSurface() => _surface;

	public IGodotSkiaSurface GetOrCreateSurface() => _surface ??= CreateSurface();

	private IEnumerable<object> GetOrCreateSurfaces() => [GetOrCreateSurface()];

	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Doesn't affect correctness")]
	public void SetRenderSize(PixelSize renderSize, double renderScaling)
	{
		var hasScalingChanged = RenderScaling != renderScaling;
		if (_renderSize == renderSize && !hasScalingChanged)
			return;

		var oldClientSize = ClientSize;
		var unclampedClientSize = renderSize.ToSize(renderScaling);

		ClientSize = new Size(Math.Max(unclampedClientSize.Width, 0.0), Math.Max(unclampedClientSize.Height, 0.0));
		RenderScaling = renderScaling;

		if (_renderSize != renderSize)
		{
			_renderSize = renderSize;

			if (_surface is not null)
			{
				_surface.Dispose();
				_surface = null;
			}

			if (_isDisposed)
				return;

			_surface = CreateSurface();
		}

		if (hasScalingChanged)
		{
			_surface?.RenderScaling = RenderScaling;
			ScalingChanged?.Invoke(RenderScaling);
		}

		if (oldClientSize != ClientSize)
			Resized?.Invoke(ClientSize,
				hasScalingChanged ? WindowResizeReason.DpiChange : WindowResizeReason.Unspecified);
	}

	public void OnDraw(Rect rect) => Paint?.Invoke(rect);

	public bool OnMouseMotion(InputEventMouseMotion inputEvent, ulong timestamp)
	{
		_lastMouseDeviceId = inputEvent.Device;

		if (_inputRoot is null || Input is not { } input)
			return false;

		var args = new RawPointerEventArgs(
			GodotDevices.GetMouse(inputEvent.Device),
			timestamp,
			_inputRoot,
			RawPointerEventType.Move,
			CreateRawPointerPoint(inputEvent.Position, inputEvent.Pressure, inputEvent.Tilt),
			inputEvent.GetRawInputModifiers()
		);

		input(args);

		return args.Handled;
	}

	public bool OnMouseButton(InputEventMouseButton inputEvent, ulong timestamp)
	{
		_lastMouseDeviceId = inputEvent.Device;

		if (_inputRoot is null || Input is not { } input)
			return false;

		var args = (inputEvent.ButtonIndex, inputEvent.Pressed) switch
		{
			(GdMouseButton.Left, true) => CreateButtonArgs(RawPointerEventType.LeftButtonDown),
			(GdMouseButton.Left, false) => CreateButtonArgs(RawPointerEventType.LeftButtonUp),
			(GdMouseButton.Right, true) => CreateButtonArgs(RawPointerEventType.RightButtonDown),
			(GdMouseButton.Right, false) => CreateButtonArgs(RawPointerEventType.RightButtonUp),
			(GdMouseButton.Middle, true) => CreateButtonArgs(RawPointerEventType.MiddleButtonDown),
			(GdMouseButton.Middle, false) => CreateButtonArgs(RawPointerEventType.MiddleButtonUp),
			(GdMouseButton.Xbutton1, true) => CreateButtonArgs(RawPointerEventType.XButton1Down),
			(GdMouseButton.Xbutton1, false) => CreateButtonArgs(RawPointerEventType.XButton1Up),
			(GdMouseButton.Xbutton2, true) => CreateButtonArgs(RawPointerEventType.XButton2Down),
			(GdMouseButton.Xbutton2, false) => CreateButtonArgs(RawPointerEventType.XButton2Up),
			(GdMouseButton.WheelUp, _) => CreateWheelArgs(new Vector(0.0, inputEvent.Factor)),
			(GdMouseButton.WheelDown, _) => CreateWheelArgs(new Vector(0.0, -inputEvent.Factor)),
			(GdMouseButton.WheelLeft, _) => CreateWheelArgs(new Vector(inputEvent.Factor, 0.0)),
			(GdMouseButton.WheelRight, _) => CreateWheelArgs(new Vector(-inputEvent.Factor, 0.0)),
			_ => null
		};

		if (args is null)
			return false;

		input(args);

		return args.Handled;

		RawPointerEventArgs CreateButtonArgs(RawPointerEventType type)
		{
			return new RawPointerEventArgs(
				GodotDevices.GetMouse(inputEvent.Device),
				timestamp,
				_inputRoot,
				type,
				inputEvent.Position.ToAvaloniaPoint() / RenderScaling,
				inputEvent.GetRawInputModifiers()
			);
		}

		RawMouseWheelEventArgs CreateWheelArgs(Vector delta)
		{
			return new RawMouseWheelEventArgs(
				GodotDevices.GetMouse(inputEvent.Device),
				timestamp,
				_inputRoot,
				inputEvent.Position.ToAvaloniaPoint() / RenderScaling,
				delta,
				inputEvent.GetRawInputModifiers()
			);
		}
	}

	public bool OnScreenTouch(InputEventScreenTouch inputEvent, ulong timestamp)
	{
		if (_inputRoot is null || Input is not { } input)
			return false;

		var args = new RawTouchEventArgs(
			_touchDevice,
			timestamp,
			_inputRoot,
			inputEvent.Pressed ? RawPointerEventType.TouchBegin : RawPointerEventType.TouchEnd,
			inputEvent.Position.ToAvaloniaPoint() / RenderScaling,
			InputModifiersProvider.GetRawInputModifiers(),
			inputEvent.Index
		);

		input(args);

		return args.Handled;
	}

	public bool OnScreenDrag(InputEventScreenDrag inputEvent, ulong timestamp)
	{
		if (_inputRoot is null || Input is not { } input)
			return false;

		var args = new RawTouchEventArgs(
			_touchDevice,
			timestamp,
			_inputRoot,
			RawPointerEventType.TouchUpdate,
			CreateRawPointerPoint(inputEvent.Position, inputEvent.Pressure, inputEvent.Tilt),
			inputEvent.GetRawInputModifiers(),
			inputEvent.Index
		);

		input(args);

		return args.Handled;
	}

	private RawPointerPoint CreateRawPointerPoint(Vector2 position, float pressure, Vector2 tilt) =>
		new()
		{
			Position = position.ToAvaloniaPoint() / RenderScaling,
			Twist = 0.0f,
			Pressure = pressure,
			XTilt = tilt.X * 90.0f,
			YTilt = tilt.Y * 90.0f
		};

	public bool OnKey(InputEventKey inputEvent, ulong timestamp)
	{
		if (_inputRoot is null || Input is not { } input)
			return false;

		var keyCode = inputEvent.Keycode;
		var pressed = inputEvent.Pressed;
		var key = keyCode.ToAvaloniaKey();

		if (key != AvKey.None)
		{
			var args = new RawKeyEventArgs(
				GodotDevices.Keyboard,
				timestamp,
				_inputRoot,
				pressed ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
				key,
				inputEvent.GetRawInputModifiers(),
				inputEvent.PhysicalKeycode.ToAvaloniaPhysicalKey(),
				OS.GetKeycodeString(inputEvent.KeyLabel)
			);

			input(args);

			if (args.Handled)
				return true;
		}

		if (!pressed || !OS.IsKeycodeUnicode((long)keyCode)) return false;
		{
			var text = char.ConvertFromUtf32((int)inputEvent.Unicode);
			var args = new RawTextInputEventArgs(GodotDevices.Keyboard, timestamp, _inputRoot, text);

			input(args);

			if (args.Handled)
				return true;
		}

		return false;
	}

	public bool OnJoypadButton(InputEventJoypadButton inputEvent, ulong timestamp)
	{
		if (_inputRoot is null || Input is not { } input)
			return false;

		var args = new RawJoypadButtonEventArgs(
			GodotDevices.GetJoypad(inputEvent.Device),
			timestamp,
			_inputRoot,
			inputEvent.IsPressed() ? RawJoypadButtonEventType.ButtonDown : RawJoypadButtonEventType.ButtonUp
		);

		input(args);

		return args.Handled;
	}

	public bool OnJoypadMotion(InputEventJoypadMotion inputEvent, ulong timestamp)
	{
		if (_inputRoot is null || Input is not { } input)
			return false;

		var args = new RawJoypadAxisEventArgs(
			GodotDevices.GetJoypad(inputEvent.Device),
			timestamp,
			_inputRoot
		);

		input(args);

		return args.Handled;
	}

	public void OnLostFocus() => LostFocus?.Invoke();

	public void OnMouseExited(ulong timestamp)
	{
		if (_inputRoot is null || Input is not { } input) return;

		var args = new RawPointerEventArgs(
			GodotDevices.GetMouse(_lastMouseDeviceId),
			timestamp,
			_inputRoot,
			RawPointerEventType.LeaveWindow,
			new Point(-1, -1),
			InputModifiersProvider.GetRawInputModifiers()
		);

		input(args);
	}
}
